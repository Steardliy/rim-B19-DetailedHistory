using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace DetailedHistory
{
    class GraphUtility
    {
        private static float cachedGraphTick = -1;
        private static List<SimpleCurveDrawInfo> curves = new List<SimpleCurveDrawInfo>();
        private static List<SimpleCurveDrawInfo> activeCurves = new List<SimpleCurveDrawInfo>();
        private static List<bool> curActiveLegends;
        private static Dictionary<HistoryAutoRecorderGroup, List<bool>> activeLegends = new Dictionary<HistoryAutoRecorderGroup, List<bool>>();
        public static void DrawGraph(Rect graphRect, Rect legendRect, FloatRange section, List<CurveMark> marks, HistoryAutoRecorderGroup recorderGroup)
        {
            if (Find.TickManager.TicksGame != GraphUtility.cachedGraphTick)
            {
                GraphUtility.RecalculateGraph(recorderGroup);
            }
            if (Mathf.Approximately(section.min, section.max))
            {
                section.max += 1.66666669E-05f;
            }
            SimpleCurveDrawerStyle curveDrawerStyle = Find.History.curveDrawerStyle;
            curveDrawerStyle.FixedSection = section;
            curveDrawerStyle.UseFixedScale = recorderGroup.def.useFixedScale;
            curveDrawerStyle.FixedScale = recorderGroup.def.fixedScale;
            curveDrawerStyle.YIntegersOnly = recorderGroup.def.integersOnly;
            GraphUtility.DrawCurves(graphRect, curveDrawerStyle, marks);
            GraphUtility.DrawLegends(legendRect, curveDrawerStyle);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public static void RecalculateGraph(HistoryAutoRecorderGroup recorderGroup)
        {
            GraphUtility.cachedGraphTick = Find.TickManager.TicksGame;
            GraphUtility.curves.Clear();
            GraphUtility.activeCurves.Clear();
            if (!activeLegends.TryGetValue(recorderGroup, out curActiveLegends))
            {
                curActiveLegends = new List<bool>();
                for (int i = 0; i < recorderGroup.recorders.Count; i++)
                {
                    curActiveLegends.Add(true);
                }
                activeLegends.Add(recorderGroup, curActiveLegends);
            }
            for (int i = 0; i < recorderGroup.recorders.Count; i++)
            {

                HistoryAutoRecorder historyAutoRecorder = recorderGroup.recorders[i];
                SimpleCurveDrawInfo simpleCurveDrawInfo = new SimpleCurveDrawInfo();
                simpleCurveDrawInfo.color = historyAutoRecorder.def.graphColor;
                simpleCurveDrawInfo.label = historyAutoRecorder.def.LabelCap;
                simpleCurveDrawInfo.labelY = historyAutoRecorder.def.GraphLabelY;
                simpleCurveDrawInfo.curve = new SimpleCurve();
                for (int j = 0; j < historyAutoRecorder.records.Count; j++)
                {
                    simpleCurveDrawInfo.curve.Add(new CurvePoint((float)j * (float)historyAutoRecorder.def.recordTicksFrequency / 60000f, historyAutoRecorder.records[j]), false);
                }
                simpleCurveDrawInfo.curve.SortPoints();
                if (historyAutoRecorder.records.Count == 1)
                {
                    simpleCurveDrawInfo.curve.Add(new CurvePoint(1.66666669E-05f, historyAutoRecorder.records[0]), true);
                }
                if (curActiveLegends[i])
                {
                    GraphUtility.activeCurves.Add(simpleCurveDrawInfo);
                }
                GraphUtility.curves.Add(simpleCurveDrawInfo);
            }

        }

        public static void DrawLegends(Rect rect, SimpleCurveDrawerStyle style = null)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }
            if (style == null)
            {
                style = new SimpleCurveDrawerStyle();
            }
            if (GraphUtility.curves.Count == 0)
            {
                return;
            }

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.BeginGroup(rect);
            float num = 0f;
            for(int i = 0; i < curves.Count; i++)
            {
                bool flag = curActiveLegends[i];
                Widgets.Checkbox(0f, num, ref flag, 20f);
                if(curActiveLegends[i] != flag)
                {
                    Log.Message("cghh1");
                    curActiveLegends[i] = flag;
                }
                GUI.color = curves[i].color;
                GUI.DrawTexture(new Rect(28f, num + 2f, 15f, 15f), BaseContent.WhiteTex);
                GUI.color = Color.white;
                if (curves[i].label != null)
                {
                    Widgets.Label(new Rect(46f, num, 140f, 24f), curves[i].label);
                }
                num += 24f;
            }
            GUI.EndGroup();
            GUI.color = Color.white;
        }

        private static void DrawCurves(Rect rect, SimpleCurveDrawerStyle style = null, List<CurveMark> marks = null)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }
            if (style == null)
            {
                style = new SimpleCurveDrawerStyle();
            }
            bool flag = true;
            Rect viewRect = default(Rect);
            for (int i = 0; i < activeCurves.Count; i++)
            {
                SimpleCurveDrawInfo simpleCurveDrawInfo = activeCurves[i];
                if (simpleCurveDrawInfo.curve != null)
                {
                    if (flag)
                    {
                        flag = false;
                        viewRect = simpleCurveDrawInfo.curve.View.rect;
                    }
                    else
                    {
                        viewRect.xMin = Mathf.Min(viewRect.xMin, simpleCurveDrawInfo.curve.View.rect.xMin);
                        viewRect.xMax = Mathf.Max(viewRect.xMax, simpleCurveDrawInfo.curve.View.rect.xMax);
                        viewRect.yMin = Mathf.Min(viewRect.yMin, simpleCurveDrawInfo.curve.View.rect.yMin);
                        viewRect.yMax = Mathf.Max(viewRect.yMax, simpleCurveDrawInfo.curve.View.rect.yMax);
                    }
                }
            }
            if (style.UseFixedScale)
            {
                viewRect.yMin = style.FixedScale.x;
                viewRect.yMax = style.FixedScale.y;
            }
            if (style.OnlyPositiveValues)
            {
                if (viewRect.xMin < 0f)
                {
                    viewRect.xMin = 0f;
                }
                if (viewRect.yMin < 0f)
                {
                    viewRect.yMin = 0f;
                }
            }
            if (style.UseFixedSection)
            {
                viewRect.xMin = style.FixedSection.min;
                viewRect.xMax = style.FixedSection.max;
            }
            if (Mathf.Approximately(viewRect.width, 0f) || Mathf.Approximately(viewRect.height, 0f))
            {
                return;
            }
            Rect rect2 = rect;
            if (style.DrawMeasures)
            {
                rect2.xMin += 60f;
                rect2.yMax -= 30f;
            }
            if (marks != null)
            {
                Rect rect3 = rect2;
                rect3.height = 15f;
                SimpleCurveDrawer.DrawCurveMarks(rect3, viewRect, marks);
                rect2.yMin = rect3.yMax;
            }
            if (style.DrawBackground)
            {
                GUI.color = new Color(0.302f, 0.318f, 0.365f);
                GUI.DrawTexture(rect2, BaseContent.WhiteTex);
            }
            if (style.DrawBackgroundLines)
            {
                SimpleCurveDrawer.DrawGraphBackgroundLines(rect2, viewRect);
            }
            if (style.DrawMeasures)
            {
                SimpleCurveDrawer.DrawCurveMeasures(rect, viewRect, rect2, style.MeasureLabelsXCount, style.MeasureLabelsYCount, style.XIntegersOnly, style.YIntegersOnly);
            }
            foreach (SimpleCurveDrawInfo current in activeCurves)
            {
                SimpleCurveDrawer.DrawCurveLines(rect2, current, style.DrawPoints, viewRect, style.UseAntiAliasedLines, style.PointsRemoveOptimization);
            }
            if (style.DrawCurveMousePoint)
            {
                SimpleCurveDrawer.DrawCurveMousePoint(activeCurves, rect2, viewRect, style.LabelX);
            }
        }
    }
}
