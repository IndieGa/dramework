﻿using System;

using UnityEngine.LowLevel;


namespace IG.HappyCoder.Dramework3.Runtime.Tools.Extensions
{
    public static class PlayerLoopExtensions
    {
        #region ================================ EVENTS AND DELEGATES

        public delegate void ModifyPlayerLoop(ref PlayerLoopSystem playerLoopSystem);

        #endregion

        #region ================================ METHODS

        public static void AddSystem<TSystem>(ref this PlayerLoopSystem loop, PlayerLoopSystem.UpdateFunction action)
        {
            var type = typeof(TSystem);
            loop.subSystemList = loop.subSystemList.Add(new PlayerLoopSystem
            {
                type = type,
                updateDelegate = action
            });
        }

        public static ref PlayerLoopSystem GetSystem<TSystem>(ref this PlayerLoopSystem loop)
        {
            var type = typeof(TSystem);
            for (var index = 0; index < loop.subSystemList.Length; index++)
            {
                ref var playerLoopSystem = ref loop.subSystemList[index];
                if (playerLoopSystem.type == type)
                    return ref playerLoopSystem;
            }

            throw new Exception($"Player loop system «{type}» not found");
        }

        public static void ModifyCurrentPlayerLoop(ModifyPlayerLoop modifyPlayerLoop)
        {
            var currentLoop = PlayerLoop.GetCurrentPlayerLoop();
            modifyPlayerLoop(ref currentLoop);
            PlayerLoop.SetPlayerLoop(currentLoop);
        }

        public static void RemoveSystem<TSystem>(ref this PlayerLoopSystem loop, bool recursive = true)
        {
            if (loop.subSystemList == null) return;

            var type = typeof(TSystem);
            for (var index = loop.subSystemList.Length - 1; index >= 0; index--)
            {
                ref var playerLoopSystem = ref loop.subSystemList[index];
                if (playerLoopSystem.type == type)
                    loop.subSystemList = loop.subSystemList.RemoveAt(index);
                else if (recursive)
                    playerLoopSystem.RemoveSystem<TSystem>();
            }
        }

        private static T[] Add<T>(this T[] array, T value)
        {
            if (array == null) array = new T[1];
            else Array.Resize(ref array, array.Length + 1);
            array[^1] = value;
            return array;
        }

        private static T[] RemoveAt<T>(this T[] array, int index)
        {
            if (array == null || index < 0) return array;

            var newArray = new T[array.Length - 1];
            if (index > 0)
            {
                Array.Copy(array, 0, newArray, 0, index);
            }
            if (index < newArray.Length)
            {
                Array.Copy(array, index + 1, newArray, index, array.Length - (index + 1));
            }

            return newArray;
        }

        #endregion
    }
}