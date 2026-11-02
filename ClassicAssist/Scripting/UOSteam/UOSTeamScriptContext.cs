using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ClassicAssist.Scripting.UOSteam
{
    public sealed class UOSTeamScriptContext
    {
        private readonly List<string> _lines = new List<string>();
        private readonly List<int> _sourceLineNumbers = new List<int>();
        private readonly Dictionary<int, int> _blockEndIndices = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _nextConditionalBranchIndices = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _loopStartIndices = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _forLoopRemaining = new Dictionary<int, int>();
        private readonly HashSet<int> _pendingConditionalBranches = new HashSet<int>();

        public UOSTeamScriptContext()
        {
            Runtime = new UOSTeamRuntime();
            StopRequested = false;
        }

        public string FileName { get; private set; }
        public int CurrentLineNumber { get; private set; }
        public int CurrentIndex { get; private set; }
        public UOSTeamRuntime Runtime { get; }
        public bool StopRequested { get; set; }

        public bool LoadFromText( string scriptText, string fileName = "unnamed.uos" )
        {
            if ( string.IsNullOrEmpty( scriptText ) )
            {
                return false;
            }

            FileName = fileName;
            CurrentLineNumber = 0;
            CurrentIndex = 0;
            StopRequested = false;
            _lines.Clear();
            _sourceLineNumbers.Clear();
            _forLoopRemaining.Clear();
            _pendingConditionalBranches.Clear();

            string[] sourceLines = scriptText.Replace( "\r\n", "\n" ).Replace( '\r', '\n' ).Split( '\n' );

            for ( int lineIndex = 0; lineIndex < sourceLines.Length; lineIndex++ )
            {
                foreach ( string statement in SplitStatements( sourceLines[lineIndex] ) )
                {
                    _lines.Add( statement );
                    _sourceLineNumbers.Add( lineIndex + 1 );
                }
            }

            BuildControlFlow();
            return true;
        }

        public bool LoadFromFile( string filePath )
        {
            if ( !File.Exists( filePath ) )
            {
                return false;
            }

            return LoadFromText( File.ReadAllText( filePath ), Path.GetFileName( filePath ) );
        }

        public bool ExecuteNext( UOSTeamCommandRegistry registry )
        {
            if ( registry == null )
            {
                throw new ArgumentNullException( nameof( registry ) );
            }

            if ( CurrentIndex >= _lines.Count )
            {
                return false;
            }

            string rawLine = _lines[CurrentIndex];
            CurrentLineNumber = _sourceLineNumbers[CurrentIndex];
            CurrentIndex++;

            string line = rawLine.Trim();
            if ( string.IsNullOrWhiteSpace( line ) || line.StartsWith( "//", StringComparison.Ordinal ) || line.StartsWith( "#", StringComparison.Ordinal ) )
            {
                return true;
            }

            string[] tokens = NormalizeControlTokens( Split( line ) );
            if ( tokens.Length == 0 )
            {
                return true;
            }

            string command = tokens[0].ToLowerInvariant();

            switch ( command )
            {
                case "if":
                    if ( !registry.EvaluateCondition( this, tokens.Skip( 1 ).ToArray() ) )
                    {
                        JumpToNextConditionalBranch( CurrentIndex - 1 );
                    }

                    break;
                case "elseif":
                    int branchIndex = CurrentIndex - 1;

                    if ( !_pendingConditionalBranches.Remove( branchIndex ) )
                    {
                        CurrentIndex = _blockEndIndices[branchIndex] + 1;
                        break;
                    }

                    if ( !registry.EvaluateCondition( this, tokens.Skip( 1 ).ToArray() ) )
                    {
                        JumpToNextConditionalBranch( branchIndex );
                    }

                    break;
                case "else":
                    int elseIndex = CurrentIndex - 1;

                    if ( !_pendingConditionalBranches.Remove( elseIndex ) )
                    {
                        CurrentIndex = _blockEndIndices[elseIndex] + 1;
                    }

                    break;
                case "endif":
                    break;
                case "while":
                    if ( !registry.EvaluateCondition( this, tokens.Skip( 1 ).ToArray() ) )
                    {
                        CurrentIndex = _blockEndIndices[CurrentIndex - 1] + 1;
                    }

                    break;
                case "endwhile":
                    CurrentIndex = _loopStartIndices[CurrentIndex - 1];
                    break;
                case "for":
                    int forStartIndex = CurrentIndex - 1;
                    if ( !_forLoopRemaining.TryGetValue( forStartIndex, out int remaining ) )
                    {
                        remaining = ParseForLoopCount( tokens.Skip( 1 ).ToArray() );

                        if ( remaining == 0 )
                        {
                            CurrentIndex = _blockEndIndices[forStartIndex] + 1;
                            break;
                        }

                        _forLoopRemaining[forStartIndex] = remaining;
                    }

                    break;
                case "endfor":
                    int forLoopStart = _loopStartIndices[CurrentIndex - 1];
                    remaining = _forLoopRemaining[forLoopStart] - 1;

                    if ( remaining > 0 )
                    {
                        _forLoopRemaining[forLoopStart] = remaining;
                        CurrentIndex = forLoopStart + 1;
                    }
                    else
                    {
                        _forLoopRemaining.Remove( forLoopStart );
                    }

                    break;
                default:
                    string executableCommand = tokens[0];
                    if ( executableCommand.StartsWith( "@", StringComparison.Ordinal ) )
                    {
                        executableCommand = executableCommand.Substring( 1 );
                    }

                    if ( executableCommand.EndsWith( "!", StringComparison.Ordinal ) )
                    {
                        executableCommand = executableCommand.Substring( 0, executableCommand.Length - 1 );
                    }

                    registry.Execute( this, executableCommand, tokens.Skip( 1 ).ToArray() );
                    break;
            }

            return !StopRequested;
        }

        public bool ExecuteAll( UOSTeamCommandRegistry registry )
        {
            bool hasMore = true;

            while ( hasMore && !StopRequested )
            {
                hasMore = ExecuteNext( registry );
            }

            return true;
        }

        private void BuildControlFlow()
        {
            _blockEndIndices.Clear();
            _nextConditionalBranchIndices.Clear();
            _loopStartIndices.Clear();

            Stack<ControlBlock> blocks = new Stack<ControlBlock>();

            for ( int index = 0; index < _lines.Count; index++ )
            {
                string line = _lines[index].Trim();

                if ( string.IsNullOrWhiteSpace( line ) || line.StartsWith( "//", StringComparison.Ordinal ) ||
                     line.StartsWith( "#", StringComparison.Ordinal ) )
                {
                    continue;
                }

                string[] tokens = NormalizeControlTokens( Split( line ) );

                if ( tokens.Length == 0 )
                {
                    continue;
                }

                string command = tokens[0].ToLowerInvariant();

                if ( command == "if" || command == "while" || command == "for" )
                {
                    blocks.Push( new ControlBlock( command, index ) );
                }
                else if ( command == "elseif" || command == "else" )
                {
                    if ( blocks.Count == 0 || blocks.Peek().Type != "if" || blocks.Peek().HasElse )
                    {
                        throw CreateSyntaxException( index, $"Unexpected or duplicate {command}." );
                    }

                    ControlBlock block = blocks.Peek();

                    if ( command == "else" )
                    {
                        block.HasElse = true;
                    }

                    block.ConditionalBranches.Add( index );
                }
                else if ( command == "endif" || command == "endwhile" || command == "endfor" )
                {
                    string expectedType = command == "endif" ? "if" : command == "endwhile" ? "while" : "for";

                    if ( blocks.Count == 0 || blocks.Peek().Type != expectedType )
                    {
                        throw CreateSyntaxException( index, $"Unexpected {command}." );
                    }

                    ControlBlock block = blocks.Pop();
                    _blockEndIndices[block.StartIndex] = index;

                    int nextBranch = index;

                    for ( int branchIndex = block.ConditionalBranches.Count - 1; branchIndex >= 0; branchIndex-- )
                    {
                        int currentBranch = block.ConditionalBranches[branchIndex];
                        _nextConditionalBranchIndices[currentBranch] = nextBranch;
                        _blockEndIndices[currentBranch] = index;
                        nextBranch = currentBranch;
                    }

                    _nextConditionalBranchIndices[block.StartIndex] = nextBranch;

                    if ( expectedType == "while" || expectedType == "for" )
                    {
                        _loopStartIndices[index] = block.StartIndex;
                    }
                }
            }

            if ( blocks.Count > 0 )
            {
                ControlBlock block = blocks.Peek();
                string closingCommand = block.Type == "if" ? "endif" : block.Type == "while" ? "endwhile" : "endfor";
                throw CreateSyntaxException( block.StartIndex, $"Missing {closingCommand}." );
            }
        }

        private InvalidOperationException CreateSyntaxException( int lineIndex, string message )
        {
            int sourceLine = lineIndex >= 0 && lineIndex < _sourceLineNumbers.Count
                ? _sourceLineNumbers[lineIndex]
                : lineIndex + 1;
            return new InvalidOperationException( $"{message} File: {FileName}; Line: {sourceLine}." );
        }

        private void JumpToNextConditionalBranch( int branchIndex )
        {
            int nextBranch = _nextConditionalBranchIndices[branchIndex];

            if ( nextBranch < _blockEndIndices[branchIndex] )
            {
                _pendingConditionalBranches.Add( nextBranch );
                CurrentIndex = nextBranch;
            }
            else
            {
                CurrentIndex = nextBranch + 1;
            }
        }

        private int ParseForLoopCount( string[] args )
        {
            if ( args.Length != 1 && args.Length != 3 ||
                 args.Length == 3 && !string.Equals( args[1], "to", StringComparison.OrdinalIgnoreCase ) )
            {
                throw CreateSyntaxException( CurrentIndex - 1, "Invalid for loop. Use 'for count' or 'for start to end'." );
            }

            int count;

            if ( args.Length == 1 )
            {
                if ( !int.TryParse( args[0], out count ) )
                {
                    throw CreateSyntaxException( CurrentIndex - 1, "For loop count must be an integer." );
                }
            }
            else
            {
                if ( !int.TryParse( args[0], out int start ) || !int.TryParse( args[2], out int end ) )
                {
                    throw CreateSyntaxException( CurrentIndex - 1, "For loop bounds must be integers." );
                }

                count = end - start;
            }

            if ( count < 0 )
            {
                throw CreateSyntaxException( CurrentIndex - 1, "For loop count cannot be negative." );
            }

            return count;
        }

        private static string[] NormalizeControlTokens( string[] tokens )
        {
            if ( tokens.Length > 1 && string.Equals( tokens[0], "else", StringComparison.OrdinalIgnoreCase ) &&
                 string.Equals( tokens[1], "if", StringComparison.OrdinalIgnoreCase ) )
            {
                return new[] { "elseif" }.Concat( tokens.Skip( 2 ) ).ToArray();
            }

            return tokens;
        }

        private static IEnumerable<string> SplitStatements( string line )
        {
            StringBuilder statement = new StringBuilder();
            char quote = '\0';

            foreach ( char character in line )
            {
                if ( ( character == '\'' || character == '"' ) && ( quote == '\0' || quote == character ) )
                {
                    quote = quote == '\0' ? character : '\0';
                }

                if ( character == ';' && quote == '\0' )
                {
                    yield return statement.ToString();
                    statement.Clear();
                    continue;
                }

                statement.Append( character );
            }

            yield return statement.ToString();
        }

        private sealed class ControlBlock
        {
            public ControlBlock( string type, int startIndex )
            {
                Type = type;
                StartIndex = startIndex;
                ConditionalBranches = new List<int>();
            }

            public string Type { get; }
            public int StartIndex { get; }
            public bool HasElse { get; set; }
            public List<int> ConditionalBranches { get; }
        }

        private static string[] Split( string line )
        {
            List<string> result = new List<string>();
            string token = string.Empty;
            bool insideSingleQuote = false;
            bool insideDoubleQuote = false;

            foreach ( char ch in line )
            {
                if ( ch == '\'' && !insideDoubleQuote )
                {
                    insideSingleQuote = !insideSingleQuote;

                    if ( !insideSingleQuote && token.Length > 0 )
                    {
                        result.Add( token );
                        token = string.Empty;
                    }

                    continue;
                }

                if ( ch == '"' && !insideSingleQuote )
                {
                    insideDoubleQuote = !insideDoubleQuote;

                    if ( !insideDoubleQuote && token.Length > 0 )
                    {
                        result.Add( token );
                        token = string.Empty;
                    }

                    continue;
                }

                if ( char.IsWhiteSpace( ch ) && !insideSingleQuote && !insideDoubleQuote )
                {
                    if ( token.Length > 0 )
                    {
                        result.Add( token );
                        token = string.Empty;
                    }

                    continue;
                }

                token += ch;
            }

            if ( token.Length > 0 )
            {
                result.Add( token );
            }

            return result.ToArray();
        }
    }
}
