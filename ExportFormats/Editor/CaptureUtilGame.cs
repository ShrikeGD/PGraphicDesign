#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GameExport
{
    /// <summary>
    /// Editor-only GameView resolution setter (reflection; Unity internal API).
    /// Works in Play Mode to force Game View to specific WxH.
    /// </summary>
    public static class GameViewSizeUtil
    {
        private static object _gameViewSizesInstance;
        private static MethodInfo _getGroup;
        private static MethodInfo _addCustomSize;
        private static MethodInfo _getBuiltinCount;
        private static MethodInfo _getCustomCount;
        private static MethodInfo _getGameViewSize;
        private static MethodInfo _indexOf;
        private static MethodInfo _setSize;

        private static Type _gameViewSizeType;
        private static Type _gameViewSizesType;
        private static Type _gameViewSizeGroupType;
        private static Type _gameViewType;
        private static Type _gvSizeType;
        private static Type _gvSizeTypeEnum;

        private static bool _inited;

        public static int GetCurrentSizeIndex()
        {
            EnsureInit();
            var gv = GetMainGameView();
            return (int)_getSizeSelectionIndexProp().GetValue(gv);
        }

        public static void SetSizeIndex(int index)
        {
            EnsureInit();
            var gv = GetMainGameView();
            _getSizeSelectionIndexProp().SetValue(gv, index);
            gv.Repaint();
        }

        public static int EnsureSizeAndSelect(int width, int height)
        {
            EnsureInit();

            var group = GetStandaloneGroup();
            int existingIndex = FindSizeIndex(group, width, height);
            if (existingIndex < 0)
            {
                AddCustomSize(group, width, height, $"{width}x{height}");
                existingIndex = FindSizeIndex(group, width, height);
            }

            if (existingIndex >= 0)
                SetSizeIndex(existingIndex);

            return existingIndex;
        }

        // ---------------- internals ----------------

        private static void EnsureInit()
        {
            if (_inited) return;

            _gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            _gameViewSizesType = Type.GetType("UnityEditor.GameViewSizes,UnityEditor");
            _gameViewSizeGroupType = Type.GetType("UnityEditor.GameViewSizeGroup,UnityEditor");
            _gameViewSizeType = Type.GetType("UnityEditor.GameViewSize,UnityEditor");
            _gvSizeType = Type.GetType("UnityEditor.GameViewSizeType,UnityEditor");
            _gvSizeTypeEnum = _gvSizeType;

            var singleType = Type.GetType("UnityEditor.ScriptableSingleton`1,UnityEditor");
            var generic = singleType.MakeGenericType(_gameViewSizesType);
            var instProp = generic.GetProperty("instance");
            _gameViewSizesInstance = instProp.GetValue(null);

            _getGroup = _gameViewSizesType.GetMethod("GetGroup");
            _addCustomSize = _gameViewSizeGroupType.GetMethod("AddCustomSize");
            _getBuiltinCount = _gameViewSizeGroupType.GetMethod("GetBuiltinCount");
            _getCustomCount = _gameViewSizeGroupType.GetMethod("GetCustomCount");
            _getGameViewSize = _gameViewSizeGroupType.GetMethod("GetGameViewSize");
            _indexOf = _gameViewSizeGroupType.GetMethod("IndexOf");

            _inited = true;
        }

        private static EditorWindow GetMainGameView()
        {
            // Create or fetch GameView window
            var gv = EditorWindow.GetWindow(_gameViewType);
            return gv;
        }

        private static PropertyInfo _getSizeSelectionIndexProp()
        {
            // property: selectedSizeIndex (internal)
            return _gameViewType.GetProperty("selectedSizeIndex",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        private static object GetStandaloneGroup()
        {
            // 0 == Standalone group in Unity's internal enum for GameViewSizeGroupType
            // (We avoid referencing the internal enum type directly.)
            return _getGroup.Invoke(_gameViewSizesInstance, new object[] { 0 });
        }

        private static int FindSizeIndex(object group, int w, int h)
        {
            int builtin = (int)_getBuiltinCount.Invoke(group, null);
            int custom = (int)_getCustomCount.Invoke(group, null);
            int total = builtin + custom;

            for (int i = 0; i < total; i++)
            {
                var size = _getGameViewSize.Invoke(group, new object[] { i });
                int sw = (int)_gameViewSizeType.GetProperty("width").GetValue(size);
                int sh = (int)_gameViewSizeType.GetProperty("height").GetValue(size);

                if (sw == w && sh == h)
                    return (int)_indexOf.Invoke(group, new object[] { size });
            }

            return -1;
        }

        private static void AddCustomSize(object group, int w, int h, string name)
        {
            // new GameViewSize(GameViewSizeType.FixedResolution, w, h, name)
            var ctor = _gameViewSizeType.GetConstructor(new[]
            {
                _gvSizeTypeEnum, typeof(int), typeof(int), typeof(string)
            });

            // 1 == FixedResolution in UnityEditor.GameViewSizeType
            var fixedRes = Enum.ToObject(_gvSizeTypeEnum, 1);

            var newSize = ctor.Invoke(new object[] { fixedRes, w, h, name });
            _addCustomSize.Invoke(group, new object[] { newSize });
        }
    }
}
#endif
