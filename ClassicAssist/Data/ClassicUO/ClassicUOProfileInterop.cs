#region License

// Copyright (C) 2025 Reetus
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

#endregion

using System;
using System.Reflection;
using Assistant;

namespace ClassicAssist.Data.ClassicUO
{
    /// <summary>
    ///     Writes ClassicAssist preferences into the running ClassicUO profile when possible.
    /// </summary>
    internal static class ClassicUOProfileInterop
    {
        private const string ProfileManagerTypeName = "ClassicUO.Configuration.ProfileManager";
        private const string ClientTypeName = "ClassicUO.Client";

        public static void TryPushGargoyleFlyAnimationEnabled( bool enabled )
        {
            if ( Engine.ClassicAssembly == null || !Engine.Connected )
            {
                return;
            }

            try
            {
                Type profileManagerType = Engine.ClassicAssembly.GetType( ProfileManagerTypeName );

                if ( profileManagerType == null )
                {
                    return;
                }

                object profile = profileManagerType.GetProperty( "CurrentProfile" )?.GetValue( null );

                if ( profile == null )
                {
                    return;
                }

                PropertyInfo prop = profile.GetType().GetProperty( "GargoyleFlyAnimationEnabled" );

                if ( prop == null || !prop.CanWrite )
                {
                    return;
                }

                prop.SetValue( profile, enabled );

                string path = profileManagerType.GetProperty( "ProfilePath" )?.GetValue( null ) as string;

                if ( string.IsNullOrEmpty( path ) )
                {
                    return;
                }

                Type clientType = Engine.ClassicAssembly.GetType( ClientTypeName );

                if ( clientType == null )
                {
                    return;
                }

                object game = clientType.GetProperty( "Game", BindingFlags.Public | BindingFlags.Static )
                    ?.GetValue( null );

                if ( game == null )
                {
                    return;
                }

                object uo = game.GetType().GetProperty( "UO" )?.GetValue( game );

                if ( uo == null )
                {
                    return;
                }

                object world = uo.GetType().GetProperty( "World" )?.GetValue( uo );

                if ( world == null )
                {
                    return;
                }

                MethodInfo save = profile.GetType().GetMethod( "Save", new[] { world.GetType(), typeof( string ) } );

                save?.Invoke( profile, new[] { world, path } );
            }
            catch
            {
                // Optional integration; older ClassicUO builds may omit the property.
            }
        }
    }
}
