using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Assistant;
using ClassicAssist.Data.Macros;
using ClassicAssist.Data.Macros.Commands;
using ClassicAssist.Scripting.UOSteam;
using ClassicAssist.Shared.Resources;
using ClassicAssist.UI.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClassicAssist.Tests
{
    [TestClass]
    public class MacroTests
    {
        [TestMethod]
        public void WillExecute()
        {
            MacroEntry me = new MacroEntry();

            MacroManager mi = MacroManager.GetInstance();

            mi.Execute( me );
        }

        [TestMethod]
        public void WillIncludeParameters()
        {
            MacroInvoker macroInvoker = new MacroInvoker();

            AutoResetEvent are = new AutoResetEvent( false );

            MacroEntry macro = new MacroEntry { Macro = "args[0].Set()" };

            macroInvoker.ExceptionEvent += exception => { Assert.Fail( exception.Message ); };

            macroInvoker.Execute( macro, new object[] { are } );

            bool set = are.WaitOne( 5000 );

            if ( !set )
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void WillExecuteUOScriptThroughMacroInvoker()
        {
            MacroEntry macro = new MacroEntry { Macro = "sysmsg 'UOS runtime test'\nstop" };
            Assert.IsTrue( macro.IsUOScript );

            MacroInvoker macroInvoker = new MacroInvoker();
            AutoResetEvent stopped = new AutoResetEvent( false );

            macroInvoker.ExceptionEvent += exception => Assert.Fail( exception.Message );
            macroInvoker.StoppedEvent += () => stopped.Set();

            macroInvoker.Execute( macro );

            Assert.IsTrue( stopped.WaitOne( 5000 ) );
            Assert.IsFalse( macroInvoker.IsFaulted );
        }

        [TestMethod]
        public void UOSIfElseSelectsCorrectBranch()
        {
            MacroEntry macro = new MacroEntry { Macro = "if false\nstop\nelse\nstop\nendif" };
            Assert.IsTrue( macro.IsUOScript );

            UOSTeamScriptRunner runner = new UOSTeamScriptRunner();
            Assert.IsTrue( runner.LoadScript( macro.Macro ) );

            Assert.IsTrue( runner.ExecuteAll() );
            Assert.IsTrue( runner.Context.StopRequested );
            Assert.AreEqual( 4, runner.Context.CurrentLineNumber );
        }

        [TestMethod]
        public void UOSIfSupportsElseIfAndInlineStatements()
        {
            UOSTeamScriptRunner runner = new UOSTeamScriptRunner();
            Assert.IsTrue( runner.LoadScript(
                "if false;stop;elseif true;stop;else;stop;endif" ) );

            Assert.IsTrue( runner.ExecuteAll() );
            Assert.IsTrue( runner.Context.StopRequested );
            Assert.AreEqual( 4, runner.Context.CurrentIndex );
            Assert.AreEqual( 1, runner.Context.CurrentLineNumber );
        }

        [TestMethod]
        public void UOSNormalizesElseIfAndPreservesSemicolonsInsideStrings()
        {
            UOSTeamScriptRunner runner = new UOSTeamScriptRunner();
            Assert.IsTrue( runner.LoadScript(
                "if false\nstop\nelse if true\ncreatelist 'names;with-semi'\nelse\nstop\nendif" ) );

            Assert.IsTrue( runner.ExecuteAll() );
            Assert.IsTrue( runner.Context.Runtime.HasList( "names;with-semi" ) );
        }

        [TestMethod]
        public void UOSWhileFalseSkipsItsBody()
        {
            UOSTeamScriptRunner runner = new UOSTeamScriptRunner();
            Assert.IsTrue( runner.LoadScript( "while false\nstop\nendwhile\nstop" ) );

            Assert.IsTrue( runner.ExecuteAll() );
            Assert.IsTrue( runner.Context.StopRequested );
            Assert.AreEqual( 4, runner.Context.CurrentLineNumber );
        }

        [TestMethod]
        public void UOSSupportsNestedConditionsInsideLoops()
        {
            UOSTeamScriptRunner runner = new UOSTeamScriptRunner();
            Assert.IsTrue( runner.LoadScript( "while true\nif true\nstop\nelse\nstop\nendif\nendwhile" ) );

            Assert.IsTrue( runner.ExecuteAll() );
            Assert.IsTrue( runner.Context.StopRequested );
            Assert.AreEqual( 3, runner.Context.CurrentLineNumber );
        }

        [TestMethod]
        public void UOSReportsUnclosedControlFlowBlock()
        {
            UOSTeamScriptRunner runner = new UOSTeamScriptRunner();

            InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(
                () => runner.LoadScript( "if true\nstop" ) );

            StringAssert.Contains( exception.Message, "Missing endif" );
            StringAssert.Contains( exception.Message, "Line: 1" );
        }

        [TestMethod]
        public void UOSForRunsTheRequestedNumberOfIterations()
        {
            UOSTeamScriptRunner runner = new UOSTeamScriptRunner();
            Assert.IsTrue( runner.LoadScript( "for 3\npause 1\nendfor\nstop" ) );

            Assert.IsTrue( runner.ExecuteAll() );
            Assert.IsTrue( runner.Context.StopRequested );
            Assert.AreEqual( 4, runner.Context.CurrentLineNumber );
        }

        [TestMethod]
        public void UOSForZeroSkipsItsBody()
        {
            UOSTeamScriptRunner runner = new UOSTeamScriptRunner();
            Assert.IsTrue( runner.LoadScript( "for 0\nstop\nendfor\nstop" ) );

            Assert.IsTrue( runner.ExecuteAll() );
            Assert.IsTrue( runner.Context.StopRequested );
            Assert.AreEqual( 4, runner.Context.CurrentLineNumber );
        }

        [TestMethod]
        public void UOSListsSupportCreatePushFrontPopAndRemove()
        {
            UOSTeamScriptRunner runner = new UOSTeamScriptRunner();
            Assert.IsTrue( runner.LoadScript(
                "createlist 'items'\npushlist 'items' 'back-value'\npushlist 'items' 'front-value' 'front'\npoplist 'items' 'front'\nstop" ) );

            Assert.IsTrue( runner.ExecuteAll() );
            CollectionAssert.AreEqual( new[] { "back-value" }, runner.Context.Runtime.Lists["items"] );

            UOSTeamScriptRunner removeRunner = new UOSTeamScriptRunner();
            Assert.IsTrue( removeRunner.LoadScript( "createlist 'items'\nremovelist 'items'\nstop" ) );
            Assert.IsTrue( removeRunner.ExecuteAll() );
            Assert.IsFalse( removeRunner.Context.Runtime.HasList( "items" ) );
        }

        [TestMethod]
        public void UOSSupportsTimerExistsAndNotCondition()
        {
            string timerName = "uos-test-" + Guid.NewGuid().ToString( "N" );
            UOSTeamScriptRunner runner = new UOSTeamScriptRunner();
            Assert.IsTrue( runner.LoadScript(
                $"if not timerexists '{timerName}'\ncreatetimer '{timerName}'\nsettimer '{timerName}' 0\nendif\nstop" ) );

            Assert.IsTrue( runner.ExecuteAll() );
            Assert.IsTrue( TimerCommands.TimerExists( timerName ) );
            Assert.IsTrue( runner.Context.StopRequested );
        }

        [TestMethod]
        public void UOSSupportsTimerNumericComparison()
        {
            string timerName = "uos-test-" + Guid.NewGuid().ToString( "N" );
            TimerCommands.CreateTimer( timerName );
            TimerCommands.SetTimer( timerName, 0 );

            UOSTeamScriptRunner runner = new UOSTeamScriptRunner();
            Assert.IsTrue( runner.LoadScript(
                $"if timer '{timerName}' >= 0\nstop\nelse\nsysmsg 'wrong branch'\nendif" ) );

            Assert.IsTrue( runner.ExecuteAll() );
            Assert.IsTrue( runner.Context.StopRequested );
            Assert.AreEqual( 2, runner.Context.CurrentLineNumber );
        }

        [TestMethod]
        public void UOSExampleScriptLoadsWithSupportedControlFlow()
        {
            string script =
                "if not timerexists 'esci'\n" +
                "  createtimer 'esci'\n" +
                "  settimer 'esci' 0\n" +
                "endif\n" +
                "if not timerexists 'curse'\n" +
                "  createtimer 'curse'\n" +
                "  settimer 'curse' 10000\n" +
                "endif\n" +
                "getenemy 'closest' 'nearest''grey''murderer''enemy''criminal'\n" +
                "@setability 'secondary' 'on'\n" +
                "attack 'last'\n" +
                "pause 100\n" +
                "while buffexists 'Blood Oath'\n" +
                "  warmode 'off'\n" +
                "  cast 'remove curse' 'self'\n" +
                "endwhile\n" +
                "warmode 'on'\n" +
                "attack 'last'\n" +
                "if diffhits > 50 and timer 'curse' > 5000\n" +
                "  cast 'curse weapon'\n" +
                "  settimer 'curse' 0\n" +
                "endif\n" +
                "if stam < 150\n" +
                "  cast 'divine fury'\n" +
                "  pause 100\n" +
                "endif\n" +
                "if timer 'consacra' > 5000\n" +
                "  cast \"Consecrate Weapon\"\n" +
                "  settimer 'consacra' 0\n" +
                "endif";

            Assert.IsTrue( UOSTeamScriptRunner.LooksLikeUOScript( script ) );

            UOSTeamScriptRunner runner = new UOSTeamScriptRunner();
            Assert.IsTrue( runner.LoadScript( script, "combat-test.uos" ) );
        }

        [TestMethod]
        public void UOSRegistryIncludesMappedClassicAssistActions()
        {
            UOSTeamCommandRegistry registry = new UOSTeamCommandRegistry();

            foreach ( string command in new[]
                     {
                         "useobject", "usetype", "useonce", "clearusequeue", "clearhands", "bandageself",
                         "useskill", "waitforcontents", "ping", "resync", "cast", "getenemy", "attack",
                         "clickobject", "moveitem", "moveitemoffset", "movetypeoffset", "walk", "run", "turn",
                         "setskill", "dress", "undress", "togglemounted", "ignoreobject", "clearignorelist",
                         "waitforproperties", "waitforgump", "replygump", "closegump", "waitforprompt",
                         "cancelprompt"
                     } )
            {
                Assert.IsTrue( registry.Handlers.ContainsKey( command ), $"Missing UOS command mapping: {command}" );
            }
        }

        [TestMethod]
        public void UOSConditionsCanQueryScriptLocalLists()
        {
            UOSTeamScriptRunner runner = new UOSTeamScriptRunner();
            Assert.IsTrue( runner.LoadScript(
                "createlist 'targets'\n" +
                "pushlist 'targets' 'murderer'\n" +
                "if listexists 'targets' and inlist 'targets' 'murderer' and list 'targets' == 1\n" +
                "  stop\n" +
                "else\n" +
                "  clearlist 'targets'\n" +
                "endif" ) );

            Assert.IsTrue( runner.ExecuteAll() );
            Assert.IsTrue( runner.Context.StopRequested );
        }

        [TestMethod]
        public void UOSListQueryReportsMissingLists()
        {
            UOSTeamScriptRunner runner = new UOSTeamScriptRunner();
            Assert.IsTrue( runner.LoadScript( "if inlist 'missing' 'value'\nstop\nendif" ) );

            InvalidOperationException exception =
                Assert.ThrowsException<InvalidOperationException>( () => runner.ExecuteAll() );

            StringAssert.Contains( exception.Message, "List 'missing' does not exist." );
            StringAssert.Contains( exception.Message, "Line: 1" );
        }

        [TestMethod]
        public void UOSCompatibilitySmokeTestScriptLoads()
        {
            string scriptPath = Path.GetFullPath( Path.Combine( AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..", "UOS-Test", "UOS_Compatibility_SmokeTest.uos" ) );
            Assert.IsTrue( File.Exists( scriptPath ), $"Smoke test script not found: {scriptPath}" );

            string script = File.ReadAllText( scriptPath );
            Assert.IsTrue( UOSTeamScriptRunner.LooksLikeUOScript( script ) );

            UOSTeamScriptRunner runner = new UOSTeamScriptRunner();
            Assert.IsTrue( runner.LoadScript( script, scriptPath ) );
        }

        [TestMethod]
        public void UOSDetectionSkipsLeadingCommentsAndRecognizesActionCommands()
        {
            Assert.IsTrue( UOSTeamScriptRunner.LooksLikeUOScript(
                "# UOS sample\n// comment\n@useskill 'Meditation'\nstop" ) );
        }

        [TestMethod]
        public void WontThrowExceptionNewMacroNoAssistantOptions()
        {
            try
            {
                string path = Path.Combine( Engine.StartupPath ?? Environment.CurrentDirectory, "Assistant.json" );

                if ( File.Exists( path ) )
                {
                    File.Delete( path );
                }

                MacrosTabViewModel vm = new MacrosTabViewModel();

                vm.NewMacroCommand.Execute( null );
            }
            catch ( Exception )
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void WillAbortRunning()
        {
            MacroEntry me = new MacroEntry { Macro = "while true:\r\n\t" };

            MacroManager mi = MacroManager.GetInstance();

            MacroInvoker macroInvoker = new MacroInvoker();

            macroInvoker.Execute( me );

            macroInvoker.Stop();

            bool result = macroInvoker.Thread.Join( 5000 );

            Assert.IsTrue( result );
        }

        [TestMethod]
        public void WillRaiseStartedEvent()
        {
            MacroEntry me = new MacroEntry();

            MacroManager mi = MacroManager.GetInstance();

            AutoResetEvent are = new AutoResetEvent( false );

            void OnStartedEvent()
            {
                are.Set();
            }

            MacroInvoker macroInvoker = new MacroInvoker();

            macroInvoker.StartedEvent += OnStartedEvent;

            macroInvoker.Execute( me );

            bool result = are.WaitOne( 5000 );

            Assert.IsTrue( result );
        }

        [TestMethod]
        public void WillRaiseStoppedEvent()
        {
            MacroEntry me = new MacroEntry();

            MacroManager mi = MacroManager.GetInstance();

            AutoResetEvent are = new AutoResetEvent( false );

            void OnStoppedEvent()
            {
                are.Set();
            }

            MacroInvoker macroInvoker = new MacroInvoker();

            macroInvoker.StoppedEvent += OnStoppedEvent;

            macroInvoker.Execute( me );

            bool result = are.WaitOne( 5000 );

            Assert.IsTrue( result );
        }

        [TestMethod]
        public void WillExceptionEvent()
        {
            MacroEntry me = new MacroEntry { Macro = "kjdkdsdksdfsdk" };

            AutoResetEvent are = new AutoResetEvent( false );

            MacroInvoker macroInvoker = new MacroInvoker();

            void OnExceptionEvent( Exception e )
            {
                Assert.IsTrue( macroInvoker.IsFaulted );
                Assert.IsNotNull( macroInvoker.Exception );
                are.Set();
            }

            macroInvoker.ExceptionEvent += OnExceptionEvent;

            macroInvoker.Execute( me );

            bool result = are.WaitOne( 5000 );

            Assert.IsTrue( result );
        }

        [TestMethod]
        public void EnsureAllCommandsHaveAttribute()
        {
            IEnumerable<Type> types = Assembly.GetAssembly( typeof( Engine ) ).GetTypes().Where( t =>
                t.Namespace != null && t.Namespace.EndsWith( "Macros.Commands" ) && t.IsClass && t.IsPublic &&
                !t.Name.Contains( "Dummy" ) );

            foreach ( Type type in types )
            {
                MethodInfo[] members = type.GetMethods( BindingFlags.Public | BindingFlags.Static );

                foreach ( MethodInfo member in members )
                {
                    CommandsDisplayAttribute attr = member.GetCustomAttribute<CommandsDisplayAttribute>();

                    if ( attr == null )
                    {
                        Assert.Fail( $"{type.Name}.{member.Name} has no CommandsDisplayAttribute." );
                    }
                }
            }
        }

        [TestMethod]
        public void EnsureAllCategoriesAreLocalizable()
        {
            IEnumerable<Type> types = Assembly.GetAssembly( typeof( Engine ) ).GetTypes().Where( t =>
                t.Namespace != null && t.Namespace.EndsWith( "Macros.Commands" ) && t.IsClass && t.IsPublic &&
                !t.Name.Contains( "Dummy" ) );

            foreach ( Type type in types )
            {
                MethodInfo[] members = type.GetMethods( BindingFlags.Public | BindingFlags.Static );

                foreach ( MethodInfo member in members )
                {
                    CommandsDisplayAttribute attr = member.GetCustomAttribute<CommandsDisplayAttribute>();

                    if ( attr == null )
                    {
                        Assert.Fail( $"{type.Name}.{member.Name} has no CommandsDisplayAttribute." );
                    }

                    string resourceName = Strings.ResourceManager.GetString( attr.Category );

                    if ( string.IsNullOrEmpty( resourceName ) )
                    {
                        Assert.Fail(
                            $"{type.Name}.{member.Name}: Category \"{attr.Category}\" has no resource entry." );
                    }
                }
            }
        }
    }
}