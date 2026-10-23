using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AsuRim.Armoury;

public static class PostGizmos
{
	private static readonly int[] DwellChoicesSeconds = new int[7] { 1, 2, 4, 8, 15, 30, 60 };

	public static IEnumerable<Gizmo> For(CompGuardPost post)
	{
		if (post?.parent == null)
		{
			yield break;
		}
		yield return new Command_Action
		{
			defaultLabel = ArmouryText.Get("ASA_Post_CopySettings"),
			defaultDesc = ArmouryText.Get("ASA_Post_CopySettings_Desc"),
			icon = StanceIcons.Get("UI/Commands/CopySettings", BaseContent.BadTex),
			hotKey = KeyBindingDefOf.Misc4,
			action = () =>
			{
				PostSettingsClipboard.Copy(post);
				SoundDefOf.Tick_High.PlayOneShotOnCamera();
			}
		};
		Command_Action command_Action = new Command_Action
		{
			defaultLabel = ArmouryText.Get("ASA_Post_PasteSettings"),
			defaultDesc = ArmouryText.Get("ASA_Post_PasteSettings_Desc"),
			icon = StanceIcons.Get("UI/Commands/PasteSettings", BaseContent.BadTex),
			hotKey = KeyBindingDefOf.Misc5,
			action = () =>
			{
				ApplyToSelection((CompGuardPost p) =>
				{
					PostSettingsClipboard.PasteInto(p);
				});
				SoundDefOf.Tick_High.PlayOneShotOnCamera();
			}
		};
		if (!PostSettingsClipboard.HasCopied)
		{
			command_Action.Disable();
		}
		yield return command_Action;
		yield return new Command_Action
		{
			defaultLabel = ArmouryText.Get("ASA_Post_Assign"),
			defaultDesc = ArmouryText.Get("ASA_Post_Assign_Desc"),
			icon = StanceIcons.Get("UI/Commands/AssignOwner", BaseContent.BadTex),
			action = () =>
			{
				Find.WindowStack.Add(new Dialog_AssignPost(post));
			}
		};
		yield return new Command_Action
		{
			defaultLabel = post.stance.Label(),
			defaultDesc = ArmouryText.Get("ASA_Post_Stance_Desc") + "\n\n" + post.stance.Desc(),
			icon = StanceIcons.Get(post.stance),
			action = () =>
			{
				List<FloatMenuOption> list = new List<FloatMenuOption>();
				foreach (CombatStance value in Enum.GetValues(typeof(CombatStance)))
				{
					if (value.Offerable())
					{
						CombatStance captured = value;
						Action<Rect> mouseoverGuiAction = (Rect rect) =>
						{
							TooltipHandler.TipRegion(rect, captured.LabelKey().Translate() + "\n\n" + captured.DescKey().Translate());
						};
						list.Add(new FloatMenuOption(captured.LabelKey().Translate(), () =>
						{
							ApplyToSelection((CompGuardPost p) =>
							{
								p.stance = captured;
							});
						}, StanceIcons.Get(captured), Color.white, MenuOptionPriority.Default, mouseoverGuiAction));
					}
				}
				Find.WindowStack.Add(new FloatMenu(list));
			}
		};
		yield return new Command_Action
		{
			defaultLabel = ((post.OverwatchArea != null) ? ((string)"ASA_Post_Watching".Translate(post.OverwatchArea.Label)) : ((string)"ASA_Overwatch_Off".Translate())),
			defaultDesc = "ASA_Post_Overwatch_Desc".Translate(),
			icon = OverwatchIcon(post.OverwatchArea != null),
			action = () =>
			{
				List<FloatMenuOption> list = new List<FloatMenuOption>
				{
					new FloatMenuOption("ASA_Overwatch_Off".Translate(), () =>
					{
						ApplyToSelection((CompGuardPost p) =>
						{
							p.OverwatchArea = null;
						});
					})
				};
				foreach (Area allArea in post.parent.Map.areaManager.AllAreas)
				{
					if (allArea.AssignableAsAllowed())
					{
						Area captured = allArea;
						list.Add(new FloatMenuOption(captured.Label, () =>
						{
							ApplyToSelection((CompGuardPost p) =>
							{
								p.OverwatchArea = captured;
							});
						}));
					}
				}
				if (list.Count == 1)
				{
					list.Add(new FloatMenuOption("ASA_Overwatch_NoAreas".Translate(), null));
				}
				Find.WindowStack.Add(new FloatMenu(list));
			}
		};
		if (post.Kind != PostKind.Patrol)
		{
			yield return new Command_Action
			{
				defaultLabel = "ASA_Post_Facing".Translate(post.facing.LabelKey().Translate()),
				defaultDesc = "ASA_Post_Facing_Desc".Translate(),
				icon = StanceIcons.Get("UI/Stances/Bowman_" + FacingSuffix(post.facing), BaseContent.BadTex),
				action = () =>
				{
					List<FloatMenuOption> list = new List<FloatMenuOption>();
					foreach (PostFacing value2 in Enum.GetValues(typeof(PostFacing)))
					{
						PostFacing captured = value2;
						list.Add(new FloatMenuOption(captured.LabelKey().Translate(), () =>
						{
							ApplyToSelection((CompGuardPost p) =>
							{
								p.facing = captured;
							});
						}));
					}
					Find.WindowStack.Add(new FloatMenu(list));
				}
			};
		}
		if (post.Kind != PostKind.Patrol)
		{
			yield break;
		}
		yield return new Command_Action
		{
			defaultLabel = "ASA_Post_Dwell".Translate(DwellLabel(post.dwellTicks)),
			defaultDesc = "ASA_Post_Dwell_Desc".Translate(),
			icon = StanceIcons.Get("UI/Stances/ASA_Overwatch", TexCommand.HoldOpen),
			action = () =>
			{
				List<FloatMenuOption> list = new List<FloatMenuOption>();
				int[] dwellChoicesSeconds = DwellChoicesSeconds;
				foreach (int num in dwellChoicesSeconds)
				{
					int ticks = num * 60;
					list.Add(new FloatMenuOption(DwellLabel(ticks), () =>
					{
						ApplyToSelection((CompGuardPost p) =>
						{
							if (p.Kind == PostKind.Patrol)
							{
								p.dwellTicks = ticks;
							}
						});
					}));
				}
				Find.WindowStack.Add(new FloatMenu(list));
			}
		};
	}

	private static string FacingSuffix(PostFacing facing)
	{
		return facing switch
		{
			PostFacing.North => "north", 
			PostFacing.East => "east", 
			PostFacing.West => "west", 
			_ => "south", 
		};
	}

	internal static Texture2D OverwatchIcon(bool armed)
	{
		if (!armed)
		{
			return StanceIcons.Get("UI/Stances/ASA_EyeClosed", TexCommand.HoldOpen);
		}
		return StanceIcons.Get("UI/Stances/ASA_Overwatch", TexCommand.HoldOpen);
	}

	private static string DwellLabel(int ticks)
	{
		return "ASA_Post_DwellSeconds".Translate(ticks / 60);
	}

	private static void ApplyToSelection(Action<CompGuardPost> apply)
	{
		List<object> selectedObjects = Find.Selector.SelectedObjects;
		for (int i = 0; i < selectedObjects.Count; i++)
		{
			if (selectedObjects[i] is Thing thing)
			{
				CompGuardPost compGuardPost = thing.TryGetComp<CompGuardPost>();
				if (compGuardPost != null)
				{
					apply(compGuardPost);
				}
			}
		}
	}
}
