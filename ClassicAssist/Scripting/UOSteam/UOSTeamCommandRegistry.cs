using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using ClassicAssist.UO.Objects;
using Assistant;
using ClassicAssist.Data.Macros.Commands;
using ClassicAssist.UO;
using UOC = ClassicAssist.UO.Commands;

namespace ClassicAssist.Scripting.UOSteam
{
    public sealed class UOSTeamCommandRegistry
    {
        private readonly Dictionary<string, Func<UOSTeamScriptContext, string[], bool>> _handlers =
            new Dictionary<string, Func<UOSTeamScriptContext, string[], bool>>( StringComparer.InvariantCultureIgnoreCase );

        public UOSTeamCommandRegistry()
        {
            RegisterDefaults();
        }

        public IReadOnlyDictionary<string, Func<UOSTeamScriptContext, string[], bool>> Handlers => _handlers;

        public void Register( string commandName, Func<UOSTeamScriptContext, string[], bool> handler )
        {
            _handlers[Normalize( commandName )] = handler;
        }

        public bool Execute( UOSTeamScriptContext context, string commandName, string[] args )
        {
            if ( context == null )
            {
                throw new ArgumentNullException( nameof( context ) );
            }

            string normalized = Normalize( commandName );

            if ( !_handlers.TryGetValue( normalized, out Func<UOSTeamScriptContext, string[], bool> handler ) )
            {
                throw new InvalidOperationException(
                    $"UOS command '{commandName}' is not registered in ClassicAssist. File: {context.FileName}; Line: {context.CurrentLineNumber}." );
            }

            return handler( context, args ?? Array.Empty<string>() );
        }

        public bool EvaluateCondition( UOSTeamScriptContext context, string[] tokens )
        {
            if ( tokens == null || tokens.Length == 0 )
            {
                throw new InvalidOperationException(
                    $"A UOS condition is required. File: {context.FileName}; Line: {context.CurrentLineNumber}." );
            }

            List<string[]> clauses = new List<string[]>();
            List<string> currentClause = new List<string>();
            List<string> operators = new List<string>();

            foreach ( string token in tokens )
            {
                if ( string.Equals( token, "and", StringComparison.OrdinalIgnoreCase ) ||
                     string.Equals( token, "or", StringComparison.OrdinalIgnoreCase ) )
                {
                    if ( currentClause.Count == 0 )
                    {
                        throw ConditionError( context, "A condition is missing before a logical operator." );
                    }

                    clauses.Add( currentClause.ToArray() );
                    currentClause.Clear();
                    operators.Add( token.ToLowerInvariant() );
                }
                else
                {
                    currentClause.Add( token );
                }
            }

            if ( currentClause.Count == 0 )
            {
                throw ConditionError( context, "A condition is required after a logical operator." );
            }

            clauses.Add( currentClause.ToArray() );
            bool result = false;
            bool andGroup = EvaluateConditionClause( context, clauses[0] );

            for ( int index = 0; index < operators.Count; index++ )
            {
                bool next = EvaluateConditionClause( context, clauses[index + 1] );

                if ( operators[index] == "and" )
                {
                    andGroup = andGroup && next;
                }
                else
                {
                    result = result || andGroup;
                    andGroup = next;
                }
            }

            return result || andGroup;
        }

        private bool EvaluateConditionClause( UOSTeamScriptContext context, string[] tokens )
        {
            bool negate = string.Equals( tokens[0], "not", StringComparison.OrdinalIgnoreCase );
            int commandIndex = negate ? 1 : 0;

            if ( commandIndex >= tokens.Length )
            {
                throw ConditionError( context, "A condition must follow 'not'." );
            }

            int operatorIndex = Array.FindIndex( tokens, commandIndex, IsComparisonOperator );
            bool result;

            if ( operatorIndex >= 0 )
            {
                object left = EvaluateConditionValue( context, tokens.Skip( commandIndex ).Take( operatorIndex - commandIndex ).ToArray() );
                object right = ParseConditionLiteral( context, tokens.Skip( operatorIndex + 1 ).ToArray() );
                result = CompareConditionValues( context, left, tokens[operatorIndex], right );
            }
            else
            {
                object value = EvaluateConditionValue( context, tokens.Skip( commandIndex ).ToArray() );
                result = value is bool boolean ? boolean : Convert.ToDouble( value, CultureInfo.InvariantCulture ) != 0;
            }

            return negate ? !result : result;
        }

        private object EvaluateConditionValue( UOSTeamScriptContext context, string[] tokens )
        {
            if ( tokens.Length == 0 )
            {
                throw ConditionError( context, "A condition value is required." );
            }

            if ( tokens.Length == 1 && bool.TryParse( tokens[0], out bool literal ) )
            {
                return literal;
            }

            switch ( tokens[0].ToLowerInvariant() )
            {
                case "timer":
                    if ( tokens.Length != 2 )
                    {
                        throw ConditionError( context, "timer expects one timer name." );
                    }

                    return TimerCommands.Timer( tokens[1] );
                case "stam":
                case "stamina":
                    return MobileCommands.Stam( tokens.Length > 1 ? tokens[1] : "self" );
                case "diffhits":
                    if ( tokens.Length == 1 || string.Equals( tokens[1], "self", StringComparison.OrdinalIgnoreCase ) )
                    {
                        return Engine.Player == null ? 0 : Engine.Player.HitsMax - Engine.Player.Hits;
                    }

                    return MobileCommands.DiffHits( tokens[1] );
                case "hits":
                    return MobileCommands.Hits( tokens.Length > 1 ? tokens[1] : "self" );
                case "mana":
                    return MobileCommands.Mana( tokens.Length > 1 ? tokens[1] : "self" );
                case "maxhits":
                    return MobileCommands.MaxHits( tokens.Length > 1 ? tokens[1] : "self" );
                case "maxstam":
                case "maxstamina":
                    return MobileCommands.MaxStam( tokens.Length > 1 ? tokens[1] : "self" );
                case "maxmana":
                    return MobileCommands.MaxMana( tokens.Length > 1 ? tokens[1] : "self" );
                case "weight":
                    return MobileCommands.Weight();
                case "maxweight":
                    return MobileCommands.MaxWeight();
                case "diffweight":
                    return MobileCommands.DiffWeight();
                case "followers":
                    return MobileCommands.Followers();
                case "maxfollowers":
                    return MobileCommands.MaxFollowers();
                case "gold":
                    return MobileCommands.Gold();
                case "luck":
                    return MobileCommands.Luck();
                case "str":
                    return MobileCommands.Str();
                case "dex":
                    return MobileCommands.Dex();
                case "int":
                    return MobileCommands.Int();
                case "physical":
                    return Engine.Player?.PhysicalResistance ?? 0;
                case "fire":
                    return Engine.Player?.FireResistance ?? 0;
                case "cold":
                    return Engine.Player?.ColdResistance ?? 0;
                case "poison":
                    return Engine.Player?.PoisonResistance ?? 0;
                case "energy":
                    return Engine.Player?.EnergyResistance ?? 0;
                case "dead":
                    return MobileCommands.Dead( tokens.Length > 1 ? tokens[1] : null );
                case "poisoned":
                    return MobileCommands.Poisoned( tokens.Length > 1 ? tokens[1] : "self" );
                case "war":
                    return MobileCommands.War( tokens.Length > 1 ? tokens[1] : "self" );
                case "yellowhits":
                    return MobileCommands.YellowHits( tokens.Length > 1 ? tokens[1] : "self" );
                case "hidden":
                    return MobileCommands.Hidden( tokens.Length > 1 ? tokens[1] : null );
                case "paralyzed":
                    return MobileCommands.Paralyzed( tokens.Length > 1 ? tokens[1] : "self" );
                case "mounted":
                    return MobileCommands.Mounted( tokens.Length > 1 ? tokens[1] : "self" );
                case "criminal":
                    return NotorietyCommands.Criminal( tokens.Length > 1 ? tokens[1] : "self" );
                case "gray":
                case "grey":
                    return NotorietyCommands.Gray( tokens.Length > 1 ? tokens[1] : "self" );
                case "innocent":
                    return NotorietyCommands.Innocent( tokens.Length > 1 ? tokens[1] : "self" );
                case "murderer":
                    return NotorietyCommands.Murderer( tokens.Length > 1 ? tokens[1] : "self" );
                case "enemy":
                    return NotorietyCommands.Enemy( tokens.Length > 1 ? tokens[1] : "self" );
                case "friend":
                case "infriendlist":
                    return MobileCommands.InFriendList( tokens.Length > 1 ? tokens[1] : "self" );
                case "inparty":
                    return MobileCommands.InParty( tokens.Length > 1 ? tokens[1] : "self" );
                case "name":
                    return EntityCommands.Name( tokens.Length > 1 ? tokens[1] : "self" );
                case "direction":
                case "directionname":
                    return EntityCommands.Direction( tokens.Length > 1 ? tokens[1] : "self" );
                case "distance":
                    return EntityCommands.Distance( tokens.Length > 1 ? tokens[1] : "self" );
                case "graphic":
                    return EntityCommands.Graphic( tokens.Length > 1 ? tokens[1] : "self" );
                case "hue":
                    return EntityCommands.Hue( tokens.Length > 1 ? tokens[1] : "self" );
                case "x":
                    return EntityCommands.X( tokens.Length > 1 ? tokens[1] : "self" );
                case "y":
                    return EntityCommands.Y( tokens.Length > 1 ? tokens[1] : "self" );
                case "z":
                    return EntityCommands.Z( tokens.Length > 1 ? tokens[1] : "self" );
                case "inrange":
                    if ( tokens.Length != 3 || !TryParseInt( tokens[2], out int distance ) )
                    {
                        throw ConditionError( context, "inrange expects an object and a numeric distance." );
                    }

                    return EntityCommands.InRange( ParseObjectArgument( tokens[1] ), distance );
                case "findlayer":
                    if ( tokens.Length < 2 )
                    {
                        throw ConditionError( context, "findlayer expects a layer name." );
                    }

                    return ActionCommands.FindLayer( tokens[1], tokens.Length > 2 ? ParseObjectArgument( tokens[2] ) : "self" );
                case "counttype":
                    if ( tokens.Length < 2 || !TryParseInt( tokens[1], out int countGraphic ) )
                    {
                        throw ConditionError( context, "counttype expects a numeric item graphic." );
                    }

                    object source = tokens.Length > 2 ? ParseObjectArgument( tokens[2] ) : null;
                    int countHue = tokens.Length > 3 ? ParseInt( tokens[3], -1 ) : -1;
                    return ObjectCommands.CountType( countGraphic, source, countHue );
                case "counttypeground":
                    if ( tokens.Length < 2 || !TryParseInt( tokens[1], out int groundGraphic ) )
                    {
                        throw ConditionError( context, "counttypeground expects a numeric item graphic." );
                    }

                    int groundHue = tokens.Length > 2 ? ParseInt( tokens[2], -1 ) : -1;
                    int groundRange = tokens.Length > 3 ? ParseInt( tokens[3], -1 ) : -1;
                    return ObjectCommands.CountTypeGround( groundGraphic, groundHue, groundRange );
                case "skill":
                case "skillvalue":
                    if ( tokens.Length != 2 )
                    {
                        throw ConditionError( context, $"{tokens[0]} expects one skill name." );
                    }

                    return SkillCommands.Skill( tokens[1] );
                case "skillbase":
                    if ( tokens.Length != 2 )
                    {
                        throw ConditionError( context, "skillbase expects one skill name." );
                    }

                    return SkillCommands.Skill( tokens[1], true );
                case "skillstate":
                    if ( tokens.Length != 2 )
                    {
                        throw ConditionError( context, "skillstate expects one skill name." );
                    }

                    return SkillCommands.SkillState( tokens[1] );
                case "injournal":
                    if ( tokens.Length == 1 )
                    {
                        throw ConditionError( context, "injournal expects a text search." );
                    }

                    return JournalCommands.InJournal( tokens[1] );
                case "targetexists":
                    return TargetCommands.TargetExists( tokens.Length > 1 ? tokens[1] : "Any" );
                case "serial":
                    if ( tokens.Length != 2 )
                    {
                        throw ConditionError( context, "serial expects one object serial or alias." );
                    }

                    return AliasCommands.ResolveSerial( ParseObjectArgument( tokens[1] ) );
                case "gumpexists":
                    if ( tokens.Length == 1 || string.Equals( tokens[1], "any", StringComparison.OrdinalIgnoreCase ) )
                    {
                        return Engine.GumpList.Count > 0;
                    }

                    if ( !uint.TryParse( tokens[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint gumpId ) )
                    {
                        throw ConditionError( context, "gumpexists expects a numeric gump ID or 'any'." );
                    }

                    return GumpCommands.GumpExists( gumpId );
                case "ingump":
                    if ( tokens.Length != 3 || !uint.TryParse( tokens[1], NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out uint searchGumpId ) )
                    {
                        throw ConditionError( context, "ingump expects a numeric gump ID and text." );
                    }

                    return GumpCommands.InGump( searchGumpId, tokens[2] );
                case "listexists":
                    if ( tokens.Length != 2 )
                    {
                        throw ConditionError( context, "listexists expects one list name." );
                    }

                    return context.Runtime.HasList( tokens[1] );
                case "inlist":
                    if ( tokens.Length != 3 )
                    {
                        throw ConditionError( context, "inlist expects a list name and a value." );
                    }

                    if ( !context.Runtime.Lists.TryGetValue( tokens[1], out List<string> list ) )
                    {
                        throw ConditionError( context, $"List '{tokens[1]}' does not exist." );
                    }

                    return list.Contains( tokens[2] );
                case "list":
                    if ( tokens.Length != 2 )
                    {
                        throw ConditionError( context, "list expects one list name." );
                    }

                    if ( !context.Runtime.Lists.TryGetValue( tokens[1], out List<string> namedList ) )
                    {
                        throw ConditionError( context, $"List '{tokens[1]}' does not exist." );
                    }

                    return namedList.Count;
                case "findtype":
                    return EvaluateFindType( context, tokens.Skip( 1 ).ToArray() );
                case "findobject":
                    return EvaluateFindObject( context, tokens.Skip( 1 ).ToArray() );
                default:
                    return Execute( context, tokens[0], tokens.Skip( 1 ).ToArray() );
            }
        }

        private bool EvaluateFindType( UOSTeamScriptContext context, string[] args )
        {
            AliasCommands.UnsetAlias( "found" );

            if ( args.Length == 0 )
            {
                throw ConditionError( context, "findtype requires an item graphic or list name." );
            }

            int hue = args.Length > 1 ? ParseInt( args[1], -1 ) : -1;
            object container = args.Length > 2 &&
                               !string.Equals( args[2], "ground", StringComparison.OrdinalIgnoreCase )
                ? ParseObjectArgument( args[2] )
                : null;
            int minimumAmount = -1;
            int range = 100;

            if ( args.Length > 3 )
            {
                minimumAmount = ParseInt( args[3], -1 );
            }

            if ( args.Length > 4 )
            {
                range = ParseInt( args[4], -1 );
            }

            if ( context.Runtime.Lists.TryGetValue( args[0], out List<string> graphicList ) )
            {
                foreach ( string graphicValue in graphicList )
                {
                    if ( !TryParseInt( graphicValue, out int listedGraphic ) )
                    {
                        throw ConditionError( context,
                            $"List '{args[0]}' contains a non-numeric item graphic '{graphicValue}'." );
                    }

                    if ( ObjectCommands.FindType( listedGraphic, range, container, hue, minimumAmount ) )
                    {
                        return true;
                    }
                }

                return false;
            }

            if ( !TryParseInt( args[0], out int graphic ) )
            {
                throw ConditionError( context, "findtype requires a numeric item graphic or an existing list name." );
            }

            return ObjectCommands.FindType( graphic, range, container, hue, minimumAmount );
        }

        private bool EvaluateFindObject( UOSTeamScriptContext context, string[] args )
        {
            AliasCommands.UnsetAlias( "found" );

            if ( args.Length == 0 )
            {
                throw ConditionError( context, "findobject requires an object serial or alias." );
            }

            object target = ParseObjectArgument( args[0] );
            int range = args.Length > 1 ? ParseInt( args[1], -1 ) : -1;
            object container = args.Length > 2 ? ParseObjectArgument( args[2] ) : null;

            return ObjectCommands.FindObject( target, range, container );
        }

        private static object ParseConditionLiteral( UOSTeamScriptContext context, string[] tokens )
        {
            if ( tokens.Length != 1 )
            {
                throw ConditionError( context, "The right side of a comparison must be one value." );
            }

            if ( double.TryParse( tokens[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double number ) )
            {
                return number;
            }

            if ( bool.TryParse( tokens[0], out bool boolean ) )
            {
                return boolean;
            }

            return tokens[0];
        }

        private static bool CompareConditionValues( UOSTeamScriptContext context, object left, string op, object right )
        {
            if ( left is bool || right is bool )
            {
                if ( !( left is bool leftBoolean ) || !( right is bool rightBoolean ) ||
                     op != "==" && op != "=" && op != "!=" )
                {
                    throw ConditionError( context, "Boolean values only support ==, =, and != comparisons." );
                }

                return op == "!=" ? leftBoolean != rightBoolean : leftBoolean == rightBoolean;
            }

            if ( double.TryParse( Convert.ToString( left, CultureInfo.InvariantCulture ), NumberStyles.Float,
                     CultureInfo.InvariantCulture, out double leftNumber ) &&
                 double.TryParse( Convert.ToString( right, CultureInfo.InvariantCulture ), NumberStyles.Float,
                     CultureInfo.InvariantCulture, out double rightNumber ) )
            {
                switch ( op )
                {
                    case "=":
                    case "==":
                        return leftNumber == rightNumber;
                    case "!=":
                        return leftNumber != rightNumber;
                    case "<":
                        return leftNumber < rightNumber;
                    case "<=":
                        return leftNumber <= rightNumber;
                    case ">":
                        return leftNumber > rightNumber;
                    case ">=":
                        return leftNumber >= rightNumber;
                }
            }

            int comparison = string.Compare( Convert.ToString( left, CultureInfo.InvariantCulture ),
                Convert.ToString( right, CultureInfo.InvariantCulture ), StringComparison.OrdinalIgnoreCase );

            switch ( op )
            {
                case "=":
                case "==":
                    return comparison == 0;
                case "!=":
                    return comparison != 0;
                case "<":
                    return comparison < 0;
                case "<=":
                    return comparison <= 0;
                case ">":
                    return comparison > 0;
                case ">=":
                    return comparison >= 0;
                default:
                    throw ConditionError( context, $"Unsupported comparison operator '{op}'." );
            }
        }

        private static bool IsComparisonOperator( string token )
        {
            return token == "=" || token == "==" || token == "!=" || token == "<" || token == "<=" ||
                   token == ">" || token == ">=";
        }

        private static InvalidOperationException ConditionError( UOSTeamScriptContext context, string message )
        {
            return new InvalidOperationException(
                $"{message} File: {context.FileName}; Line: {context.CurrentLineNumber}." );
        }

        private void RegisterDefaults()
        {
            Register( "msg", ( context, args ) =>
            {
                if ( args.Length == 0 )
                {
                    return false;
                }

                int hue = args.Length > 1 ? ParseInt( args[1], 0 ) : 0;
                UOC.Speak( args[0], hue );
                return true;
            } );

            Register( "sysmsg", ( context, args ) =>
            {
                if ( args.Length == 0 )
                {
                    return true;
                }

                UOC.SystemMessage( string.Join( " ", args ), true );
                return true;
            } );

            Register( "setalias", ( context, args ) =>
            {
                if ( args.Length < 2 )
                {
                    return false;
                }

                string alias = args[0];
                string value = args[1];

                if ( TryParseInt( value, out int integerValue ) )
                {
                    AliasCommands.SetAlias( alias, integerValue );
                    return true;
                }

                int resolvedAlias = AliasCommands.GetAlias( value );
                if ( resolvedAlias != 0 )
                {
                    AliasCommands.SetAlias( alias, resolvedAlias );
                    return true;
                }

                AliasCommands.SetAlias( alias, 0 );
                return true;
            } );

            Register( "clearalias", ( context, args ) =>
            {
                if ( args.Length == 0 )
                {
                    return true;
                }

                AliasCommands.UnsetAlias( args[0] );
                return true;
            } );

            Register( "findalias", ( context, args ) =>
            {
                if ( args.Length == 0 )
                {
                    return false;
                }

                int value = AliasCommands.GetAlias( args[0] );
                return value != 0;
            } );

            Register( "timerexists", ( context, args ) =>
            {
                if ( args.Length == 0 )
                {
                    return false;
                }

                return TimerCommands.TimerExists( args[0] );
            } );

            Register( "buffexists", ( context, args ) =>
            {
                return args.Length > 0 && EntityCommands.BuffExists( string.Join( " ", args ) );
            } );

            Register( "getenemy", ( context, args ) =>
            {
                if ( args.Length == 0 )
                {
                    return false;
                }

                string distance = "Next";
                List<string> notorieties = new List<string>();

                foreach ( string value in args )
                {
                    if ( string.Equals( value, "closest", StringComparison.OrdinalIgnoreCase ) ||
                         string.Equals( value, "nearest", StringComparison.OrdinalIgnoreCase ) )
                    {
                        distance = "Closest";
                    }
                    else
                    {
                        notorieties.Add( string.Equals( value, "grey", StringComparison.OrdinalIgnoreCase ) ? "Gray" : value );
                    }
                }

                return TargetCommands.GetEnemy( notorieties, distance: distance );
            } );

            Register( "setability", ( context, args ) =>
            {
                if ( args.Length == 0 )
                {
                    return false;
                }

                AbilitiesCommands.SetAbility( args[0], args.Length > 1 ? args[1] : "toggle" );
                return true;
            } );

            Register( "attack", ( context, args ) =>
            {
                if ( args.Length == 0 )
                {
                    return false;
                }

                object target = args[0];

                if ( string.Equals( args[0], "last", StringComparison.OrdinalIgnoreCase ) )
                {
                    int lastTarget = Engine.Player?.LastTargetSerial ?? 0;

                    if ( lastTarget <= 0 )
                    {
                        return false;
                    }

                    target = lastTarget;
                }

                ActionCommands.Attack( target );
                return true;
            } );

            Register( "warmode", ( context, args ) =>
            {
                MainCommands.WarMode( args.Length > 0 ? args[0] : "toggle" );
                return true;
            } );

            Register( "cast", ( context, args ) =>
            {
                if ( args.Length == 0 )
                {
                    return false;
                }

                if ( args.Length == 1 )
                {
                    SpellCommands.Cast( args[0] );
                    return true;
                }

                return SpellCommands.Cast( args[0], args[1] );
            } );

            Register( "useobject", ( context, args ) =>
            {
                if ( args.Length == 0 )
                {
                    return false;
                }

                ObjectCommands.UseObject( ParseObjectArgument( args[0] ) );
                return true;
            } );

            Register( "usetype", ( context, args ) =>
            {
                if ( args.Length == 0 || !TryParseInt( args[0], out int graphic ) )
                {
                    return false;
                }

                int hue = args.Length > 1 ? ParseInt( args[1], -1 ) : -1;
                object container = args.Length > 2 ? ParseObjectArgument( args[2] ) : null;
                ObjectCommands.UseType( graphic, hue, container );
                return true;
            } );

            Register( "useonce", ( context, args ) =>
            {
                if ( args.Length == 0 || !TryParseInt( args[0], out int graphic ) )
                {
                    return false;
                }

                int hue = args.Length > 1 ? ParseInt( args[1], -1 ) : -1;
                return ActionCommands.UseOnce( graphic, hue );
            } );

            Register( "clickobject", ( context, args ) =>
            {
                if ( args.Length != 1 )
                {
                    throw ConditionError( context, "clickobject expects one object serial or alias." );
                }

                ActionCommands.ClickObject( ParseObjectArgument( args[0] ) );
                return true;
            } );

            Register( "moveitem", ( context, args ) =>
            {
                if ( args.Length < 2 )
                {
                    throw ConditionError( context, "moveitem expects an item and a destination serial." );
                }

                int destination = TryParseInt( args[1], out int parsedDestination ) ? parsedDestination : 0;
                object item = ParseObjectArgument( args[0] );
                object destinationObject = ParseObjectArgument( args[1] );
                int amount = args.Length == 3 ? ParseInt( args[2], -1 ) :
                    args.Length > 5 ? ParseInt( args[5], -1 ) : -1;

                if ( destination == -1 )
                {
                    if ( args.Length > 4 )
                    {
                        if ( !TryParseInt( args[2], out int xOffset ) ||
                             !TryParseInt( args[3], out int yOffset ) ||
                             !TryParseInt( args[4], out int zOffset ) )
                        {
                            throw ConditionError( context, "moveitem ground offsets must be numeric." );
                        }

                        ObjectCommands.MoveItemOffset( item, xOffset, yOffset, zOffset, amount );
                    }
                    else
                    {
                        ObjectCommands.MoveItemOffset( item, 0, 0, 0, amount );
                    }

                    return true;
                }

                int x = args.Length > 4 ? ParseInt( args[2], -1 ) : -1;
                int y = args.Length > 4 ? ParseInt( args[3], -1 ) : -1;
                ObjectCommands.MoveItem( item, destinationObject, amount, x, y );
                return true;
            } );

            Register( "moveitemoffset", ( context, args ) =>
            {
                if ( args.Length < 4 ||
                     !TryParseInt( args[1], out int xOffset ) ||
                     !TryParseInt( args[2], out int yOffset ) ||
                     !TryParseInt( args[3], out int zOffset ) )
                {
                    throw ConditionError( context, "moveitemoffset expects an object and three numeric offsets." );
                }

                int amount = args.Length > 4 ? ParseInt( args[4], -1 ) : -1;
                ObjectCommands.MoveItemOffset( ParseObjectArgument( args[0] ), xOffset, yOffset, zOffset, amount );
                return true;
            } );

            Register( "movetypeoffset", ( context, args ) =>
            {
                if ( args.Length < 5 ||
                     !TryParseInt( args[0], out int graphic ) ||
                     !TryParseInt( args[2], out int xOffset ) ||
                     !TryParseInt( args[3], out int yOffset ) ||
                     !TryParseInt( args[4], out int zOffset ) )
                {
                    throw ConditionError( context, "movetypeoffset expects a graphic, source, and three numeric offsets." );
                }

                object source = string.Equals( args[1], "ground", StringComparison.OrdinalIgnoreCase )
                    ? null
                    : ParseObjectArgument( args[1] );
                int amount = args.Length > 5 ? ParseInt( args[5], -1 ) : -1;
                int hue = args.Length > 6 ? ParseInt( args[6], -1 ) : -1;
                int range = args.Length > 7 ? ParseInt( args[7], -1 ) : -1;
                return ObjectCommands.MoveTypeOffset( graphic, source, xOffset, yOffset, zOffset, amount, hue, range );
            } );

            Register( "walk", ( context, args ) =>
            {
                if ( args.Length != 1 )
                {
                    throw ConditionError( context, "walk expects one direction." );
                }

                return MovementCommands.Walk( NormalizeDirection( context, args[0] ) );
            } );

            Register( "run", ( context, args ) =>
            {
                if ( args.Length != 1 )
                {
                    throw ConditionError( context, "run expects one direction." );
                }

                return MovementCommands.Run( NormalizeDirection( context, args[0] ) );
            } );

            Register( "turn", ( context, args ) =>
            {
                if ( args.Length != 1 )
                {
                    throw ConditionError( context, "turn expects one direction." );
                }

                MovementCommands.Turn( NormalizeDirection( context, args[0] ) );
                return true;
            } );

            Register( "clearusequeue", ( context, args ) =>
            {
                ObjectCommands.ClearObjectQueue();
                return true;
            } );

            Register( "clearhands", ( context, args ) =>
            {
                ActionCommands.ClearHands( args.Length > 0 ? args[0] : "both" );
                return true;
            } );

            Register( "bandageself", ( context, args ) => ConsumeCommands.BandageSelf() );

            Register( "useskill", ( context, args ) =>
            {
                if ( args.Length == 0 )
                {
                    return false;
                }

                SkillCommands.UseSkill( string.Join( " ", args ) );
                return true;
            } );

            Register( "setskill", ( context, args ) =>
            {
                if ( args.Length < 2 )
                {
                    throw ConditionError( context, "setskill expects a skill name and a lock state." );
                }

                SkillCommands.SetSkill( args[0], args[1] );
                return true;
            } );

            Register( "dress", ( context, args ) =>
            {
                AgentCommands.Dress( args.Length > 0 ? string.Join( " ", args ) : null );
                return true;
            } );

            Register( "undress", ( context, args ) =>
            {
                if ( args.Length == 0 )
                {
                    throw ConditionError( context, "undress expects an agent name." );
                }

                AgentCommands.Undress( string.Join( " ", args ) );
                return true;
            } );

            Register( "togglemounted", ( context, args ) =>
            {
                ActionCommands.ToggleMounted();
                return true;
            } );

            Register( "ignoreobject", ( context, args ) =>
            {
                if ( args.Length == 0 )
                {
                    throw ConditionError( context, "ignoreobject expects an object serial or alias." );
                }

                ObjectCommands.IgnoreObject( ParseObjectArgument( args[0] ) );
                return true;
            } );

            Register( "clearignorelist", ( context, args ) =>
            {
                ObjectCommands.ClearIgnoreList();
                return true;
            } );

            Register( "waitforproperties", ( context, args ) =>
            {
                if ( args.Length < 2 || !TryParseInt( args[1], out int timeout ) )
                {
                    throw ConditionError( context, "waitforproperties expects an object and a numeric timeout." );
                }

                return PropertiesCommands.WaitForProperties( ParseObjectArgument( args[0] ), timeout );
            } );

            Register( "waitforgump", ( context, args ) =>
            {
                uint gumpId = 0;
                if ( args.Length > 0 && !uint.TryParse( args[0], NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out gumpId ) )
                {
                    throw ConditionError( context, "waitforgump expects a numeric gump ID." );
                }

                int timeout = args.Length > 1 ? ParseInt( args[1], 30000 ) : 30000;
                return GumpCommands.WaitForGump( gumpId, timeout );
            } );

            Register( "replygump", ( context, args ) =>
            {
                if ( args.Length < 2 || !uint.TryParse( args[0], NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out uint gumpId ) ||
                     !TryParseInt( args[1], out int buttonId ) )
                {
                    throw ConditionError( context, "replygump expects a numeric gump ID and button ID." );
                }

                GumpCommands.ReplyGump( gumpId, buttonId );
                return true;
            } );

            Register( "closegump", ( context, args ) =>
            {
                if ( args.Length != 1 || !TryParseInt( args[0], out int serial ) )
                {
                    throw ConditionError( context, "closegump expects a numeric gump serial." );
                }

                GumpCommands.CloseGump( serial );
                return true;
            } );

            Register( "waitforprompt", ( context, args ) =>
            {
                int timeout = args.Length > 0 ? ParseInt( args[0], 30000 ) : 30000;
                return MsgCommands.WaitForPrompt( timeout );
            } );

            Register( "cancelprompt", ( context, args ) =>
            {
                MsgCommands.CancelPrompt();
                return true;
            } );

            Register( "waitforcontents", ( context, args ) =>
            {
                if ( args.Length == 0 )
                {
                    return false;
                }

                int timeout = args.Length > 1 ? ParseInt( args[1], 5000 ) : 5000;
                return ActionCommands.WaitForContents( ParseObjectArgument( args[0] ), timeout );
            } );

            Register( "ping", ( context, args ) =>
            {
                ActionCommands.Ping();
                return true;
            } );

            Register( "resync", ( context, args ) =>
            {
                MainCommands.Resync();
                return true;
            } );

            Register( "unsetalias", ( context, args ) =>
            {
                if ( args.Length == 0 )
                {
                    return false;
                }

                AliasCommands.UnsetAlias( args[0] );
                return true;
            } );

            Register( "createlist", ( context, args ) =>
            {
                if ( args.Length == 0 )
                {
                    return false;
                }

                context.Runtime.CreateList( args[0] );
                return true;
            } );

            Register( "pushlist", ( context, args ) =>
            {
                if ( args.Length < 2 || args.Length > 3 )
                {
                    return false;
                }

                if ( !context.Runtime.HasList( args[0] ) )
                {
                    throw new InvalidOperationException(
                        $"UOS list '{args[0]}' does not exist. File: {context.FileName}; Line: {context.CurrentLineNumber}." );
                }

                if ( args.Length == 3 && !string.Equals( args[2], "front", StringComparison.OrdinalIgnoreCase ) &&
                     !string.Equals( args[2], "back", StringComparison.OrdinalIgnoreCase ) )
                {
                    throw new InvalidOperationException(
                        $"pushlist position must be 'front' or 'back'. File: {context.FileName}; Line: {context.CurrentLineNumber}." );
                }

                if ( args.Length == 3 && string.Equals( args[2], "front", StringComparison.OrdinalIgnoreCase ) )
                {
                    context.Runtime.Lists[args[0]].Insert( 0, args[1] );
                }
                else
                {
                    context.Runtime.AddToList( args[0], args[1] );
                }

                return true;
            } );

            Register( "poplist", ( context, args ) =>
            {
                if ( args.Length < 1 || args.Length > 2 )
                {
                    return false;
                }

                bool front = args.Length == 2 && string.Equals( args[1], "front", StringComparison.OrdinalIgnoreCase );
                return context.Runtime.PopList( args[0], front );
            } );

            Register( "clearlist", ( context, args ) =>
            {
                if ( args.Length == 0 )
                {
                    return false;
                }

                context.Runtime.ClearList( args[0] );
                return true;
            } );

            Register( "removelist", ( context, args ) =>
            {
                if ( args.Length == 0 )
                {
                    return false;
                }

                context.Runtime.RemoveList( args[0] );
                return true;
            } );

            Register( "pause", ( context, args ) =>
            {
                int millis = args.Length > 0 ? ParseInt( args[0], 0 ) : 0;
                Thread.Sleep( millis );
                return true;
            } );

            Register( "wait", ( context, args ) =>
            {
                int millis = args.Length > 0 ? ParseInt( args[0], 0 ) : 0;
                Thread.Sleep( millis );
                return true;
            } );

            Register( "waitfortarget", ( context, args ) =>
            {
                int timeout = args.Length > 0 ? ParseInt( args[0], 5000 ) : 5000;
                return TargetCommands.WaitForTarget( timeout );
            } );

            Register( "target", ( context, args ) =>
            {
                if ( args.Length == 0 )
                {
                    return false;
                }

                TargetCommands.Target( args[0], false, false, -1 );
                return true;
            } );

            Register( "journal", ( context, args ) =>
            {
                if ( args.Length == 0 )
                {
                    return false;
                }

                string text = string.Join( " ", args );
                return Engine.Journal.FindAny( e => e.Text.IndexOf( text, StringComparison.InvariantCultureIgnoreCase ) >= 0, "main" );
            } );

            Register( "waitforjournal", ( context, args ) =>
            {
                if ( args.Length < 2 )
                {
                    return false;
                }

                return JournalCommands.WaitForJournal( args[0], ParseInt( args[1], 0 ) );
            } );

            Register( "clearjournal", ( context, args ) =>
            {
                Engine.Journal.Clear( "main" );
                return true;
            } );

            Register( "settimer", ( context, args ) =>
            {
                if ( args.Length == 0 )
                {
                    return false;
                }

                int milliseconds = args.Length > 1 ? ParseInt( args[1], 0 ) : 0;
                TimerCommands.SetTimer( args[0], milliseconds );
                return true;
            } );

            Register( "timer", ( context, args ) =>
            {
                if ( args.Length == 0 )
                {
                    return false;
                }

                TimerCommands.Timer( args[0] );
                return true;
            } );

            Register( "removetimer", ( context, args ) =>
            {
                if ( args.Length == 0 )
                {
                    return true;
                }

                TimerCommands.RemoveTimer( args[0] );
                return true;
            } );

            Register( "createtimer", ( context, args ) =>
            {
                if ( args.Length == 0 )
                {
                    return true;
                }

                TimerCommands.CreateTimer( args[0] );
                return true;
            } );

            Register( "stop", ( context, args ) =>
            {
                context.StopRequested = true;
                return true;
            } );
        }

        private static int ParseInt( string value, int defaultValue )
        {
            return TryParseInt( value, out int parsed ) ? parsed : defaultValue;
        }

        private static bool TryParseInt( string value, out int result )
        {
            if ( int.TryParse( value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result ) )
            {
                return true;
            }

            return value != null && value.StartsWith( "0x", StringComparison.OrdinalIgnoreCase ) &&
                   int.TryParse( value.Substring( 2 ), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result );
        }

        private static object ParseObjectArgument( string value )
        {
            if ( string.Equals( value, "last", StringComparison.OrdinalIgnoreCase ) )
            {
                return Engine.Player?.LastTargetSerial ?? 0;
            }

            return TryParseInt( value, out int serial ) ? (object) serial : value;
        }

        private static string NormalizeDirection( UOSTeamScriptContext context, string direction )
        {
            switch ( direction.ToLowerInvariant() )
            {
                case "north":
                    return "North";
                case "northeast":
                case "right":
                    return "Northeast";
                case "east":
                    return "East";
                case "southeast":
                case "down":
                    return "Southeast";
                case "south":
                    return "South";
                case "southwest":
                case "left":
                    return "Southwest";
                case "west":
                    return "West";
                case "northwest":
                case "up":
                    return "Northwest";
                default:
                    throw ConditionError( context, $"Unknown direction '{direction}'." );
            }
        }

        private static string Normalize( string value )
        {
            return value == null ? string.Empty : value.Trim();
        }
    }
}
