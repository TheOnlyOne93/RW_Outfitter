﻿namespace Outfitter.TabPatch
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using KillfaceTools.FMO;

    using RimWorld;

    using UnityEngine;

    using Verse;

    public static class ITab_Bills_Patch
    {
        private static float viewHeight = 1000f;

        private static Vector2 scrollPosition = default(Vector2);

        private static readonly Vector2 WinSize = new Vector2(420f, 480f);

        private static Bill mouseoverBill;

        // RimWorld.ITab_Bills

        public static bool FillTab_Prefix()
        {
            Building_WorkTable selTable = (Building_WorkTable)Find.Selector.SingleSelectedThing;
            if (selTable.def != ThingDef.Named("HandTailoringBench")
                && selTable.def != ThingDef.Named("ElectricTailoringBench"))
            {
                return true;
            }
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.BillsTab, KnowledgeAmount.FrameDisplayed);
            float x = WinSize.x;
            Vector2 winSize2 = WinSize;
            Rect rect2 = new Rect(0f, 0f, x, winSize2.y).ContractedBy(10f);

            Func<Dictionary<string, List<FloatMenuOption>>> labeledSortingActions = delegate
                {
                    Dictionary<string, List<FloatMenuOption>> list = new Dictionary<string, List<FloatMenuOption>>();

                    for (int i = 0; i < selTable.def.AllRecipes.Count; i++)
                    {
                        if (selTable.def.AllRecipes[i].AvailableNow)
                        {
                            RecipeDef recipe = selTable.def.AllRecipes[i];
                            bool hasPart = false;

                            FloatMenuOption floatMenuOption = new FloatMenuOption(
                                recipe.LabelCap,
                                delegate
                                    {
                                        if (!selTable.Map.mapPawns.FreeColonists.Any(
                                                col => recipe.PawnSatisfiesSkillRequirements(col)))
                                        {
                                            Bill.CreateNoPawnsWithSkillDialog(recipe);
                                        }

                                        Bill bill = recipe.MakeNewBill();
                                        selTable.billStack.AddBill(bill);
                                        if (recipe.conceptLearned != null)
                                        {
                                            PlayerKnowledgeDatabase.KnowledgeDemonstrated(
                                                recipe.conceptLearned,
                                                KnowledgeAmount.Total);
                                        }

                                        if (TutorSystem.TutorialMode)
                                        {
                                            TutorSystem.Notify_Event("AddBill-" + recipe.LabelCap);
                                        }
                                    },
                                MenuOptionPriority.Default,
                                null,
                                null,
                                29f,
                                rect => Widgets.InfoCardButton(
                                    (float)(rect.x + 5.0),
                                    (float)(rect.y + (rect.height - 24.0) / 2.0),
                                    recipe));

                            //  list.Add(new FloatMenuOption("LoL", null));
                            // Outfitter jump in here

                            //  for (int j = 0; j < recipe.products.Count; j++)
                            //  {
                            //
                            ThingCountClass rec = recipe.products[0];
                            int count = selTable.Map.listerThings.AllThings.FindAll(thing => thing.def == rec.thingDef).Count;

                            for (int k = 0; k < rec?.thingDef?.apparel?.bodyPartGroups?.Count; k++)
                            {
                                BodyPartGroupDef bPart = rec.thingDef.apparel.bodyPartGroups[k];
                                hasPart = true;

                                string key = bPart.LabelCap + " ►";

                                if (!list.ContainsKey(key))
                                {
                                    list.Add(key, new List<FloatMenuOption>());
                                }
                                if (k == 0)
                                {
                                    floatMenuOption.Label += " (" + count + ")";
                                }
                                list[key].Add(floatMenuOption);
                            }

                            if (!hasPart)
                            {
                                list.Add(
                                    recipe.LabelCap,
                                    new List<FloatMenuOption>() { floatMenuOption });
                            }
                        }
                    }

                    if (!list.Any())
                    {
                        list.Add("NoneBrackets".Translate(), new List<FloatMenuOption>() { null });
                    }

                    return list;
                };

            mouseoverBill = DoListing(selTable.BillStack, rect2, labeledSortingActions, ref scrollPosition, ref viewHeight);

            return false;
        }

        public static bool TabUpdate_Prefix()
        {
            Building_WorkTable selTable = (Building_WorkTable)Find.Selector.SingleSelectedThing;
            if (selTable.def != ThingDef.Named("HandTailoringBench")
                && selTable.def != ThingDef.Named("ElectricTailoringBench"))
            {
                return true;
            }

            if (mouseoverBill != null)
            {
                mouseoverBill.TryDrawIngredientSearchRadiusOnMap(Find.Selector.SingleSelectedThing.Position);
                mouseoverBill = null;
            }
            return false;
        }

        public static Bill DoListing(BillStack __instance, Rect rect, Func<Dictionary<string, List<FloatMenuOption>>> labeledSortingActions, ref Vector2 scrollPosition, ref float viewHeight)
        {
            Bill result = null;
            GUI.BeginGroup(rect);
            Text.Font = GameFont.Small;
            if (__instance.Count < 15)
            {
                Rect rect2 = new Rect(0f, 0f, 150f, 29f);
                if (Widgets.ButtonText(rect2, "AddBill".Translate(), true, false, true))
                {
                    // Outfitter Code

                    List<FloatMenuOption> items = labeledSortingActions.Invoke().Keys.Select(
                        label =>
                            {
                                List<FloatMenuOption> fmo = labeledSortingActions.Invoke()[label];
                                return Tools.MakeMenuItemForLabel(label, fmo);
                            }).ToList();

                    Tools.LabelMenu = new FloatMenuLabels(items);
                    Find.WindowStack.Add(Tools.LabelMenu);

                    // Vanilla
                    //   Find.WindowStack.Add(new FloatMenu(recipeOptionsMaker()));
                }
                UIHighlighter.HighlightOpportunity(rect2, "AddBill");
            }
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            Rect outRect = new Rect(0f, 35f, rect.width, (float)(rect.height - 35.0));
            Rect viewRect = new Rect(0f, 0f, (float)(outRect.width - 16.0), viewHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect, true);
            float num = 0f;
            for (int i = 0; i < __instance.Count; i++)
            {
                Bill bill = __instance.Bills[i];

                Rect rect3 = bill.DoInterface(0f, num, viewRect.width, i);
                if (!bill.DeletedOrDereferenced && Mouse.IsOver(rect3))
                {
                    result = bill;
                }
                num = (float)(num + (rect3.height + 6.0));
            }
            if (Event.current.type == EventType.Layout)
            {
                viewHeight = (float)(num + 60.0);
            }
            Widgets.EndScrollView();
            GUI.EndGroup();
            return result;
        }

    }
}