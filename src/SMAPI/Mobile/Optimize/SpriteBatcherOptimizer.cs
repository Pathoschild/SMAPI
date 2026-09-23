using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;

namespace StardewModdingAPI.Mobile.Optimizer;

[HarmonyPatch]
internal static class SpriteBatcherOptimizer
{
    static Stopwatch st = new();
    static object currentBatcher;
    static Type SpriteBatcher_Type = AccessTools.TypeByName("Microsoft.Xna.Framework.Graphics.SpriteBatcher");
    static FieldInfo _batchItemCount_FieldInfo;
    static FieldInfo _batchItemList_FieldInfo;
    static FieldInfo SpriteBatchItem_Texture_FieldInfo;
    static Type SpriteBatchItem_Type = AccessTools.TypeByName("Microsoft.Xna.Framework.Graphics.SpriteBatchItem");

    static FieldInfo SpriteBatchItem_vertexTL_FieldInfo;
    static FieldInfo SpriteBatchItem_vertexTR_FieldInfo;
    static FieldInfo SpriteBatchItem_vertexBL_FieldInfo;
    static FieldInfo SpriteBatchItem_vertexBR_FieldInfo;

    public static void Init(Harmony hp)
    {
        _batchItemCount_FieldInfo = AccessTools.Field(SpriteBatcher_Type, nameof(_batchItemCount));
        _batchItemList_FieldInfo = AccessTools.Field(SpriteBatcher_Type, nameof(_batchItemList));
        SpriteBatchItem_Texture_FieldInfo = AccessTools.Field(SpriteBatchItem_Type, "Texture");
        SpriteBatchItem_vertexTL_FieldInfo = AccessTools.Field(SpriteBatchItem_Type, "vertexTL");
        SpriteBatchItem_vertexTR_FieldInfo = AccessTools.Field(SpriteBatchItem_Type, "vertexTR");
        SpriteBatchItem_vertexBL_FieldInfo = AccessTools.Field(SpriteBatchItem_Type, "vertexBL");
        SpriteBatchItem_vertexBR_FieldInfo = AccessTools.Field(SpriteBatchItem_Type, "vertexBR");
        //SpriteBatchItem

        hp.Patch(
            original: AccessTools.Method(SpriteBatcher_Type, "DrawBatch"),
            prefix: new(typeof(SpriteBatcherOptimizer), nameof(Prerfix_DrawBatch)),
            postfix: new(typeof(SpriteBatcherOptimizer), nameof(Postfix_DrawBatch))
        );
    }


    static int _batchItemCount
    {
        get => (int)_batchItemCount_FieldInfo.GetValue(currentBatcher);
        set => _batchItemCount_FieldInfo.SetValue(currentBatcher, value);
    }
    static object[] _batchItemList
    {
        get => _batchItemList_FieldInfo.GetValue(currentBatcher) as object[];
        set => _batchItemList_FieldInfo.SetValue(currentBatcher, value);
    }
    static GraphicsDevice currentGraphicsDevice;
    static FieldInfo _graphicsMetrics_FieldInfo = AccessTools.Field(SpriteBatcher_Type, "_graphicsMetrics");
    static GraphicsMetrics _graphicsMetrics
    {
        get => (GraphicsMetrics)_graphicsMetrics_FieldInfo.GetValue(currentGraphicsDevice);
        set => _graphicsMetrics_FieldInfo.SetValue(currentGraphicsDevice, value);
    }

    static MethodInfo FlushVertexArray_MethodInfo = AccessTools.Method(SpriteBatcher_Type, "FlushVertexArray");
    static unsafe void DrawBatchOptimize(SpriteSortMode sortMode, Effect effect)
    {
        if (effect != null && effect.IsDisposed)
        {
            throw new ObjectDisposedException("effect");
        }
        if (_batchItemCount == 0)
        {
            return;
        }
        var batchItems = _batchItemList;
        if ((uint)(sortMode - 2) <= 2u)
        {
            Array.Sort(batchItems, 0, _batchItemCount);
        }
        int batchIndex = 0;
        int batchCount = _batchItemCount;
        if (currentGraphicsDevice == null)
        {
            currentGraphicsDevice = AccessTools.Field(SpriteBatcher_Type, "_device")
                .GetValue(currentBatcher) as GraphicsDevice;
        }

        //TODO
        //currentGraphicsDevice._graphicsMetrics._spriteCount += batchCount;

        while (batchCount > 0)
        {
            int startIndex = 0;
            int index = 0;
            Texture2D tex = null;
            int numBatchesToProcess = batchCount;
            if (numBatchesToProcess > 5461)
            {
                numBatchesToProcess = 5461;
            }

            var _vertexArray_FieldInfo = AccessTools.Field(SpriteBatcher_Type, "_vertexArray");
            var _vertexArray = (VertexPositionColorTexture[])_vertexArray_FieldInfo.GetValue(currentBatcher);

            fixed (VertexPositionColorTexture* vertexArrayFixedPtr = _vertexArray)
            {
                VertexPositionColorTexture* vertexArrayPtr = vertexArrayFixedPtr;
                int i = 0;
                while (i < numBatchesToProcess)
                {
                    object item = batchItems[batchIndex];
                    var itemTexture = (Texture2D)SpriteBatchItem_Texture_FieldInfo.GetValue(item);
                    if (itemTexture != tex)
                    {
                        FlushVertexArray(startIndex, index, effect, tex);
                        tex = itemTexture;
                        startIndex = (index = 0);
                        vertexArrayPtr = vertexArrayFixedPtr;
                        currentGraphicsDevice.Textures[0] = tex;
                    }
                    *vertexArrayPtr = (VertexPositionColorTexture)SpriteBatchItem_vertexTL_FieldInfo.GetValue(item);
                    vertexArrayPtr[1] = (VertexPositionColorTexture)SpriteBatchItem_vertexTR_FieldInfo.GetValue(item);
                    vertexArrayPtr[2] = (VertexPositionColorTexture)SpriteBatchItem_vertexBL_FieldInfo.GetValue(item);
                    vertexArrayPtr[3] = (VertexPositionColorTexture)SpriteBatchItem_vertexBR_FieldInfo.GetValue(item);
                    SpriteBatchItem_Texture_FieldInfo.SetValue(item, null);
                    i++;
                    batchIndex++;
                    index += 4;
                    vertexArrayPtr += 4;
                }
            }
            FlushVertexArray(startIndex, index, effect, tex);
            batchCount -= numBatchesToProcess;
        }
        _batchItemCount = 0;
    }
    static void FlushVertexArray(int start, int end, Effect effect, Texture texture)
    {
        FlushVertexArray_MethodInfo.Invoke(currentBatcher, [start, end, effect, texture]);
    }

    public static unsafe bool Prerfix_DrawBatch(object __instance, SpriteSortMode sortMode, Effect effect)
    {
        currentBatcher = __instance;

        st.Restart();

        //do you want optimize?
#if false
        DrawBatchOptimize(sortMode, effect);
        return false;
#endif
        return true;
    }
    public static unsafe void Postfix_DrawBatch(SpriteBatch __instance, SpriteSortMode sortMode, Effect effect)
    {
        st.Stop();
        if (st.Elapsed.TotalMilliseconds >= 1)
        {
            Console.WriteLine($"Warn!! Draw Batch: {st.Elapsed.TotalMilliseconds}ms");
        }
    }
}
