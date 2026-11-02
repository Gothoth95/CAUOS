using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ClassicAssist.Scripting.UOSteam
{
    public sealed class UOSTeamRuntime
    {
        private readonly Dictionary<string, int> _aliases = new Dictionary<string, int>( StringComparer.InvariantCultureIgnoreCase );
        private readonly Dictionary<string, long> _timers = new Dictionary<string, long>( StringComparer.InvariantCultureIgnoreCase );
        private readonly Dictionary<string, List<string>> _lists = new Dictionary<string, List<string>>( StringComparer.InvariantCultureIgnoreCase );
        private readonly Dictionary<string, object> _variables = new Dictionary<string, object>( StringComparer.InvariantCultureIgnoreCase );

        public Dictionary<string, int> Aliases => _aliases;
        public Dictionary<string, long> Timers => _timers;
        public Dictionary<string, List<string>> Lists => _lists;
        public Dictionary<string, object> Variables => _variables;

        public string NormalizeName( string value )
        {
            return value == null ? string.Empty : value.Trim();
        }

        public void SetAlias( string alias, int value )
        {
            _aliases[NormalizeName( alias )] = value;
        }

        public void ClearAlias( string alias )
        {
            _aliases.Remove( NormalizeName( alias ) );
        }

        public int GetAlias( string alias )
        {
            string normalized = NormalizeName( alias );

            return _aliases.TryGetValue( normalized, out int value ) ? value : 0;
        }

        public bool HasAlias( string alias )
        {
            return _aliases.ContainsKey( NormalizeName( alias ) );
        }

        public void SetVariable( string name, object value )
        {
            _variables[NormalizeName( name )] = value;
        }

        public object GetVariable( string name )
        {
            string normalized = NormalizeName( name );

            return _variables.TryGetValue( normalized, out object value ) ? value : null;
        }

        public void SetTimer( string name, long milliseconds )
        {
            _timers[NormalizeName( name )] = milliseconds;
        }

        public void CreateTimer( string name )
        {
            _timers[NormalizeName( name )] = DateTime.UtcNow.Ticks;
        }

        public void RemoveTimer( string name )
        {
            _timers.Remove( NormalizeName( name ) );
        }

        public bool TimerExists( string name )
        {
            return _timers.ContainsKey( NormalizeName( name ) );
        }

        public long GetTimerMilliseconds( string name )
        {
            string normalized = NormalizeName( name );

            if ( !_timers.TryGetValue( normalized, out long startTicks ) )
            {
                return 0;
            }

            return (DateTime.UtcNow.Ticks - startTicks) / TimeSpan.TicksPerMillisecond;
        }

        public void CreateList( string name )
        {
            string normalized = NormalizeName( name );

            if ( !_lists.ContainsKey( normalized ) )
            {
                _lists[normalized] = new List<string>();
            }
        }

        public void AddToList( string name, string item )
        {
            string normalized = NormalizeName( name );

            if ( !_lists.ContainsKey( normalized ) )
            {
                throw new InvalidOperationException( $"UOS list '{normalized}' does not exist." );
            }

            _lists[normalized].Add( item );
        }

        public bool PopList( string name, bool front )
        {
            string normalized = NormalizeName( name );

            if ( !_lists.TryGetValue( normalized, out List<string> list ) )
            {
                throw new InvalidOperationException( $"UOS list '{normalized}' does not exist." );
            }

            if ( list.Count == 0 )
            {
                return false;
            }

            list.RemoveAt( front ? 0 : list.Count - 1 );
            return list.Count > 0;
        }

        public void ClearList( string name )
        {
            string normalized = NormalizeName( name );

            if ( _lists.ContainsKey( normalized ) )
            {
                _lists[normalized].Clear();
            }
        }

        public bool HasList( string name )
        {
            return _lists.ContainsKey( NormalizeName( name ) );
        }

        public void RemoveList( string name )
        {
            _lists.Remove( NormalizeName( name ) );
        }

        public string ToUOSLiteral( object value )
        {
            if ( value == null )
            {
                return string.Empty;
            }

            if ( value is bool b )
            {
                return b ? "true" : "false";
            }

            if ( value is IFormattable formattable )
            {
                return formattable.ToString( null, CultureInfo.InvariantCulture );
            }

            return value.ToString();
        }
    }
}
