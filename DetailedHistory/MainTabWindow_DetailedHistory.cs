using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace DetailedHistory
{
    public class MainTabWindow_DetailedHistory : MainTabWindow
    {
        private const float GraphAreaHeight = 450f;
        private const float GraphItemAreaWidth = 100f;
        private const float GraphLegendAreaWidth = 150f;
        private const float FramePadding = 4f;

        private enum HistoryTab : byte
        {
            Graph,
            Ranking,
            Misc
        }
        private MainTabWindow_DetailedHistory.HistoryTab curTab = HistoryTab.Graph;
        private HistoryAutoRecorderGroup historyAutoRecorderGroup;
        private static List<CurveMark> marks = new List<CurveMark>();
        private FloatRange graphSection;
        private float sliderValue = 0;
        private Vector2 graphItemScrollPosition = default(Vector2);

        public override Vector2 RequestedTabSize
        {
            get
            {
                return new Vector2(1010f, 640f);
            }
        }

        public override void PreOpen()
        {
            base.PreOpen();
            this.historyAutoRecorderGroup = Find.History.Groups().FirstOrDefault<HistoryAutoRecorderGroup>();
            if (this.historyAutoRecorderGroup != null)
            {
                this.graphSection = new FloatRange(0f, (float)Find.TickManager.TicksGame / 60000f);
            }
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                maps[i].wealthWatcher.ForceRecount(false);
            }
        }

        public override void DoWindowContents(Rect baseRect)
        {
            base.DoWindowContents(baseRect);
            Rect rect = baseRect;
            rect.yMin += 45f;
            List<TabRecord> tabList = new List<TabRecord>();
            tabList.Add(new TabRecord("Graph".Translate(), delegate
            {
                this.curTab = MainTabWindow_DetailedHistory.HistoryTab.Graph;
            }, this.curTab == MainTabWindow_DetailedHistory.HistoryTab.Graph));
            tabList.Add(new TabRecord("Ranking".Translate(), delegate
            {
                this.curTab = MainTabWindow_DetailedHistory.HistoryTab.Ranking;
            }, this.curTab == MainTabWindow_DetailedHistory.HistoryTab.Ranking));

            TabDrawer.DrawTabs(rect, tabList);

            rect.yMin += 17f;
            switch (curTab)
            {
                case HistoryTab.Graph:
                    this.DoGraphPage(rect);
                    break;
                case HistoryTab.Ranking:
                    this.DoGraphPage(rect);
                    break;
                case HistoryTab.Misc:
                    this.DoGraphPage(rect);
                    break;
            }
        }

        private void DoGraphPage(Rect rect)
        {
            GUI.BeginGroup(rect);
            Rect graphRect = new Rect(0f, 0f, rect.width - GraphItemAreaWidth - GraphLegendAreaWidth - FramePadding, GraphAreaHeight);
            Rect legendRect = new Rect(graphRect.xMax + FramePadding, graphRect.yMin, GraphLegendAreaWidth - FramePadding, GraphAreaHeight);
            if (this.historyAutoRecorderGroup != null)
            {
                MainTabWindow_DetailedHistory.marks.Clear();
                List<Tale> allTalesListForReading = Find.TaleManager.AllTalesListForReading;
                for (int i = 0; i < allTalesListForReading.Count; i++)
                {
                    Tale tale = allTalesListForReading[i];
                    if (tale.def.type == TaleType.PermanentHistorical)
                    {
                        float x = (float)GenDate.TickAbsToGame(tale.date) / 60000f;
                        MainTabWindow_DetailedHistory.marks.Add(new CurveMark(x, tale.ShortSummary, tale.def.historyGraphColor));
                    }
                }
                GraphUtility.DrawGraph(graphRect, legendRect,this.graphSection, MainTabWindow_DetailedHistory.marks, this.historyAutoRecorderGroup);
            }
            Text.Font = GameFont.Small;
            float num = (float)Find.TickManager.TicksGame / 60000f;
            string str;
            float daysMin;
            if(this.sliderValue > 300f)
            {
                str = "AllDays";
                daysMin = 0f;
            }
            else
            {
                str = "Last {0}Days".Translate(this.sliderValue);
                daysMin = Mathf.Max(0f, num - this.sliderValue);
            }
            Rect horizontalRect = new Rect(0f, GraphAreaHeight, 300f, 40f);
            this.sliderValue = Widgets.HorizontalSlider(horizontalRect, this.sliderValue, 5, 301f, true, str);
            
            this.graphSection = new FloatRange(daysMin, num);

            List<HistoryAutoRecorderGroup> list2 = Find.History.Groups();
            Rect outRect = new Rect(legendRect.xMax + FramePadding, graphRect.yMin, GraphItemAreaWidth - FramePadding, GraphAreaHeight);
            Rect viewRect = new Rect(0f, 2f, outRect.width, 24f * list2.Count);
            GUI.color = Color.white;
            Widgets.DrawBox(outRect, 1);
            Widgets.BeginScrollView(outRect, ref this.graphItemScrollPosition, viewRect, true);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(new Rect(0f, 2f, viewRect.width, 9999f));
            for (int i = 0; i < list2.Count; i++)
            {
                HistoryAutoRecorderGroup groupLocal = list2[i];
                if (listing.RadioButton(groupLocal.def.LabelCap, this.historyAutoRecorderGroup == groupLocal))
                {
                    this.historyAutoRecorderGroup = groupLocal;
                    GraphUtility.RecalculateGraph(groupLocal);
                    PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.HistoryTab, KnowledgeAmount.Total);
                }
            }
            listing.End();
            Widgets.EndScrollView();
            GUI.EndGroup();
        }
    }
}
