using System;
using System.Linq;

namespace ClassicAssist.Scripting.UOSteam
{
    public sealed class UOSTeamScriptRunner
    {
        private static readonly UOSTeamCommandRegistry DetectionRegistry = new UOSTeamCommandRegistry();
        private static readonly string[] ControlFlowCommands =
            { "if", "else", "elseif", "endif", "while", "endwhile", "for", "endfor" };
        private readonly UOSTeamCommandRegistry _commandRegistry = new UOSTeamCommandRegistry();
        private UOSTeamScriptContext _context;

        public UOSTeamScriptContext Context => _context;

        public static bool LooksLikeUOScript( string scriptText )
        {
            if ( string.IsNullOrWhiteSpace( scriptText ) )
            {
                return false;
            }

            foreach ( string rawLine in scriptText.Replace( "\r\n", "\n" ).Split( '\n' ) )
            {
                string line = rawLine.Trim();

                if ( string.IsNullOrWhiteSpace( line ) || line.StartsWith( "//", StringComparison.Ordinal ) ||
                     line.StartsWith( "#", StringComparison.Ordinal ) )
                {
                    continue;
                }

                string command = line.Split( (char[]) null, StringSplitOptions.RemoveEmptyEntries ).FirstOrDefault();
                command = command?.TrimStart( '@' );
                if ( command != null && command.EndsWith( "!", StringComparison.Ordinal ) )
                {
                    command = command.Substring( 0, command.Length - 1 );
                }

                return command != null &&
                       ( DetectionRegistry.Handlers.ContainsKey( command ) ||
                         ControlFlowCommands.Contains( command, StringComparer.InvariantCultureIgnoreCase ) );
            }

            return false;
        }

        public bool LoadFile( string filePath )
        {
            if ( string.IsNullOrWhiteSpace( filePath ) )
            {
                return false;
            }

            _context = new UOSTeamScriptContext();
            return _context.LoadFromFile( filePath );
        }

        public bool LoadScript( string scriptText, string fileName = "unnamed.uos" )
        {
            if ( string.IsNullOrWhiteSpace( scriptText ) )
            {
                return false;
            }

            _context = new UOSTeamScriptContext();
            return _context.LoadFromText( scriptText, fileName );
        }

        public bool ExecuteNext()
        {
            if ( _context == null )
            {
                return false;
            }

            return _context.ExecuteNext( _commandRegistry );
        }

        public bool ExecuteAll()
        {
            if ( _context == null )
            {
                return false;
            }

            _context.ExecuteAll( _commandRegistry );
            return true;
        }

        public void Stop()
        {
            if ( _context != null )
            {
                _context.StopRequested = true;
            }
        }
    }
}
