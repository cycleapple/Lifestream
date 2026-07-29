using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.UI;
using Lifestream.Data;
using Lifestream.Tasks.Shortcuts;
using Lumina.Excel.Sheets;
using NightmareUI;
using NightmareUI.Censoring;
using NightmareUI.PrimaryUI;
using System.Globalization;
using TerraFX.Interop.Windows;
using Action = System.Action;

namespace Lifestream.GUI;

internal static unsafe class UISettings
{
    private static string AddNew = "";
    internal static void Draw()
    {
        NuiTools.ButtonTabs([[new("一般", () => Wrapper(DrawGeneral)), new("浮動介面", () => Wrapper(DrawOverlay))], [new("進階", () => Wrapper(DrawExpert)), new("服務帳號", () => Wrapper(UIServiceAccount.Draw)), new("移動封鎖", TabTravelBan.Draw)]]);
    }

    private static void Wrapper(Action action)
    {
        ImGui.Dummy(new(5f));
        action();
    }

    private static void DrawGeneral()
    {
        new NuiBuilder()
        .Section("傳送設定")
        .Widget(() =>
        {
            ImGui.SetNextItemWidth(200f.Scale());
            ImGuiEx.EnumCombo($"跨界移動時使用的主要水晶", ref C.WorldChangeAetheryte, Lang.WorldChangeAetherytes);
            ImGuiEx.HelpMarker($"選擇跨伺服器時要先傳送到哪座主要水晶");
            ImGui.Checkbox($"跨伺服器／資料中心後傳送到指定的都市傳送網目的地", ref C.WorldVisitTPToAethernet);
            if(C.WorldVisitTPToAethernet)
            {
                ImGui.Indent();
                ImGui.SetNextItemWidth(250f.Scale());
                ImGui.InputText("都市傳送網目的地（與「/li」指令輸入方式相同）", ref C.WorldVisitTPTarget, 50);
                ImGui.Checkbox($"只在使用指令時傳送，不套用於浮動介面", ref C.WorldVisitTPOnlyCmd);
                ImGui.Unindent();
            }
            ImGui.Checkbox($"在伊修加德基礎層主要水晶加入蒼天街目的地", ref C.Firmament);
            ImGui.Checkbox($"跨伺服器前自動退出非跨界隊伍", ref C.LeavePartyBeforeWorldChange);
            ImGui.Checkbox($"在聊天欄顯示傳送目的地", ref C.DisplayChatTeleport);
            ImGui.Checkbox($"以彈出通知顯示傳送目的地", ref C.DisplayPopupNotifications);
            ImGui.Checkbox("同資料中心跨界失敗時重試", ref C.RetryWorldVisit);
            ImGui.Indent();
            ImGui.SetNextItemWidth(100f.Scale());
            ImGui.InputInt("重試間隔（秒）##2", ref C.RetryWorldVisitInterval.ValidateRange(1, 120));
            ImGui.SameLine();
            ImGuiEx.Text("＋最多");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100f.Scale());
            ImGui.InputInt("秒##2", ref C.RetryWorldVisitIntervalDelta.ValidateRange(0, 120));
            ImGuiEx.HelpMarker("隨機增加等待時間，使操作較自然");
            ImGui.Unindent();
            //ImGui.Checkbox("Use Return instead of Teleport when possible", ref C.UseReturn);
            //ImGuiEx.HelpMarker("This includes any IPC calls");
            ImGui.Checkbox("移動完成時顯示系統匣通知", ref C.EnableNotifications);
            ImGuiEx.PluginAvailabilityIndicator([new("NotificationMaster")]);
        })

        .Section("捷徑")
        .Widget(() =>
        {
            ImGui.SetNextItemWidth(200f.Scale());
            ImGuiEx.EnumCombo("「/li」指令行為", ref C.LiCommandBehavior);
            ImGui.Checkbox("傳送到自己的公寓時進入室內", ref C.EnterMyApartment);
            ImGui.SetNextItemWidth(150f.Scale());
            ImGuiEx.EnumCombo("傳送到個人／公會房屋後執行", ref C.HouseEnterMode);
            ImGui.SetNextItemWidth(150f.Scale());
            if(ImGui.BeginCombo("偏好的旅館", Utils.GetInnNameFromTerritory(C.PreferredInn), ImGuiComboFlags.HeightLarge))
            {
                foreach(var x in (uint[])[0, .. TaskPropertyShortcut.InnData.Keys])
                {
                    if(ImGui.Selectable(Utils.GetInnNameFromTerritory(x), x == C.PreferredInn)) C.PreferredInn = x;
                }
                ImGui.EndCombo();
            }
            if(Player.CID != 0)
            {
                ImGui.SetNextItemWidth(150f.Scale());
                var pref = C.PreferredSharedEstates.SafeSelect(Player.CID);
                var name = pref switch
                {
                    (0, 0, 0) => "第一個可用房屋",
                    (-1, 0, 0) => "停用",
                    _ => $"{ExcelTerritoryHelper.GetName((uint)pref.Territory)}, W{pref.Ward}, P{pref.Plot}"
                };
                if(ImGui.BeginCombo($"{Player.NameWithWorld} 偏好的共享房屋", name))
                {
                    foreach(var x in Svc.AetheryteList.Where(x => x.IsSharedHouse))
                    {
                        if(ImGui.RadioButton("第一個可用房屋", pref == default))
                        {
                            C.PreferredSharedEstates.Remove(Player.CID);
                        }
                        if(ImGui.RadioButton("停用", pref == (-1, 0, 0)))
                        {
                            C.PreferredSharedEstates[Player.CID] = (-1, 0, 0);
                        }
                        if(ImGui.RadioButton($"{ExcelTerritoryHelper.GetName(x.TerritoryId)}，第 {x.Ward} 區，第 {x.Plot} 號地", pref == ((int)x.TerritoryId, x.Ward, x.Plot)))
                        {
                            C.PreferredSharedEstates[Player.CID] = ((int)x.TerritoryId, x.Ward, x.Plot);
                        }
                    }
                    ImGui.EndCombo();
                }
            }
            ImGui.Separator();
            ImGuiEx.Text("「/li auto」指令優先順序：");
            var prio = C.PropertyPrio;
            var custom = C.PropertyPrioOverrides.TryGetValue(Player.CID, out var value);
            if(custom)
            {
                prio = value;
            }

            if(Player.Available)
            {
                ImGuiEx.Text($"{Censor.Character(Player.NameWithWorld)}：");
                ImGui.Indent();
                if(ImGui.RadioButton("使用全域設定", !custom))
                {
                    C.PropertyPrioOverrides.Remove(Player.CID);
                }
                ImGui.SameLine();

                if(ImGui.RadioButton("使用角色個別設定", custom))
                {
                    if(!custom)
                    {
                        C.PropertyPrioOverrides[Player.CID] = [];
                    }
                }
                ImGui.Unindent();
            }
            else
            {
                ImGuiEx.TextV($"正在編輯全域設定：");
            }
            ImGui.SameLine();
            if(ImGuiEx.IconButton($"\uf2ea"))
            {
                prio.Clear();
            }
            ImGuiEx.Tooltip("重設為預設順序");

            var dragDrop = Ref<ImGuiEx.RealtimeDragDrop<AutoPropertyData>>.Get(() => new("apddd", x => x.Type.ToString()));
            prio.AddRange(Enum.GetValues<TaskPropertyShortcut.PropertyType>().Where(x => x != TaskPropertyShortcut.PropertyType.Auto && !prio.Any(s => s.Type == x)).Select(x => new AutoPropertyData(false, x)));
            dragDrop.Begin();
            for(var i = 0; i < prio.Count; i++)
            {
                var d = prio[i];
                ImGui.PushID($"c{i}");
                dragDrop.NextRow();
                dragDrop.DrawButtonDummy(d, prio, i);
                ImGui.SameLine();
                ImGui.Checkbox($"{d.Type}", ref d.Enabled);
                ImGui.PopID();
            }
            dragDrop.End();

            ImGui.Separator();
        })

        .Section("地圖整合")
        .Widget(() =>
        {
            ImGui.Checkbox("點擊地圖上的都市傳送網碎晶即可快速傳送", ref C.UseMapTeleport);
            ImGui.Checkbox("僅在同一張地圖且位於主要水晶旁時處理", ref C.DisableMapClickOtherTerritory);
        })

        .Section("指令自動完成")
        .Widget(() =>
        {
            ImGuiEx.Text($"在聊天欄輸入 Lifestream 指令時顯示自動完成建議");
            ImGui.Checkbox("啟用", ref C.EnableAutoCompletion);
            ImGui.Checkbox("在固定位置顯示建議視窗", ref C.AutoCompletionFixedWindow);
            ImGui.Indent();
            ImGui.SetNextItemWidth(200f.Scale());
            ImGui.DragFloat2("位置", ref C.AutoCompletionWindowOffset, 1f);
            ImGuiEx.RadioButtonBool("從下方", "從上方", ref C.AutoCompletionWindowBottom, sameLine: true, inverted: true);
            ImGuiEx.RadioButtonBool("從右側", "從左側", ref C.AutoCompletionWindowRight, sameLine: true, inverted: true);
            ImGui.Unindent();
        })

        .Section("跨資料中心")
        .Widget(() =>
        {
            ImGui.Checkbox($"允許前往其他資料中心", ref C.AllowDcTransfer);
            ImGui.Checkbox($"切換資料中心前退出隊伍", ref C.LeavePartyBeforeLogout);
            ImGui.Checkbox($"若不在休息區，切換資料中心前先傳送到主要水晶", ref C.TeleportToGatewayBeforeLogout);
            ImGui.Checkbox($"完成資料中心移動後傳送到主要水晶", ref C.DCReturnToGateway);
            ImGui.Checkbox($"資料中心移動時允許改用其他伺服器", ref C.DcvUseAlternativeWorld);
            ImGuiEx.HelpMarker("若目的伺服器無法進入，但目標資料中心的其他伺服器可用，將先選擇可用伺服器，登入後再排入一般跨界移動。");
            ImGui.Checkbox($"目的伺服器無法進入時重試資料中心移動", ref C.EnableDvcRetry);
            ImGui.Indent();
            ImGui.SetNextItemWidth(150f.Scale());
            ImGui.InputInt("最大重試次數", ref C.MaxDcvRetries.ValidateRange(1, int.MaxValue));
            ImGui.SetNextItemWidth(150f.Scale());
            ImGui.InputInt("重試間隔（秒）", ref C.DcvRetryInterval.ValidateRange(10, 1000));
            ImGui.Unindent();
        })

        .Section("通訊錄")
        .Widget(() =>
        {
            ImGui.Checkbox($"停用前往房屋地號的自動尋路", ref C.AddressNoPathing);
            ImGuiEx.HelpMarker($"角色會停在最接近該住宅區的傳送網碎晶");
            ImGui.Checkbox($"不自動進入公寓", ref C.AddressApartmentNoEntry);
            ImGuiEx.HelpMarker($"角色會停在進入確認對話框");
        })

        .Section("移動")
        .Checkbox("自動移動時使用坐騎", () => ref C.UseMount)
        .Widget(() =>
        {
            Dictionary<int, string> mounts = [new KeyValuePair<int, string>(0, "隨機坐騎"), .. Svc.Data.GetExcelSheet<Mount>().Where(x => x.Singular != "").ToDictionary(x => (int)x.RowId, x => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(x.Singular.GetText()))];
            ImGui.SetNextItemWidth(200f);
            ImGuiEx.Combo("偏好的坐騎", ref C.Mount, mounts.Keys, names: mounts);
        })
        .Checkbox("抵達房屋地號時下坐騎", () => ref C.AutoDismount)
        .Checkbox("自動移動時使用衝刺", () => ref C.UseSprintPeloton)
        .Checkbox("自動移動時使用速行", () => ref C.UsePeloton)

        .Section("角色選擇畫面")
        .Checkbox("允許從角色選擇畫面進行資料中心及伺服器移動", () => ref C.AllowDCTravelFromCharaSelect)
        .Checkbox("前往訪客資料中心的同資料中心伺服器時使用跨界移動", () => ref C.UseGuestWorldTravel)

        .Section("Wotsit 整合")
        .Widget(() =>
        {
            var anyChanged = ImGui.Checkbox("啟用 Wotsit 整合，以傳送至都市傳送網目的地", ref C.WotsitIntegrationEnabled);
            ImGuiEx.PluginAvailabilityIndicator([new("Dalamud.FindAnything", "Wotsit")]);

            if(C.WotsitIntegrationEnabled)
            {
                ImGui.Indent();
                if(ImGui.Checkbox("包含伺服器選擇視窗", ref C.WotsitIntegrationIncludes.WorldSelect))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("包含自動傳送至房產", ref C.WotsitIntegrationIncludes.PropertyAuto))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("包含傳送至私人房屋", ref C.WotsitIntegrationIncludes.PropertyPrivate))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("包含傳送至公會房屋", ref C.WotsitIntegrationIncludes.PropertyFreeCompany))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("包含傳送至公寓", ref C.WotsitIntegrationIncludes.PropertyApartment))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("包含傳送至旅館房間", ref C.WotsitIntegrationIncludes.PropertyInn))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("包含傳送至所屬大國防聯軍", ref C.WotsitIntegrationIncludes.GrandCompany))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("包含傳送至市場佈告板", ref C.WotsitIntegrationIncludes.MarketBoard))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("包含傳送至無人島", ref C.WotsitIntegrationIncludes.IslandSanctuary))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("包含自動傳送至都市傳送網目的地", ref C.WotsitIntegrationIncludes.AetheryteAethernet))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("包含通訊錄項目", ref C.WotsitIntegrationIncludes.AddressBook))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("包含自訂別名", ref C.WotsitIntegrationIncludes.CustomAlias))
                {
                    anyChanged = true;
                }
                ImGui.Unindent();
            }

            if(anyChanged)
            {
                PluginLog.Debug("Wotsit integration settings changed, re-initializing immediately");
                S.Ipc.WotsitManager.TryClearWotsit();
                S.Ipc.WotsitManager.MaybeTryInit(true);
            }
        })

        .Draw();
    }

    private static void DrawOverlay()
    {
        new NuiBuilder()
        .Section("一般浮層設定")
        .Widget(() =>
        {
            ImGui.Checkbox("啟用浮層", ref C.Enable);
            if(C.Enable)
            {
                ImGui.Indent();
                ImGui.Checkbox("顯示都市傳送網選單", ref C.ShowAethernet);
                ImGui.Checkbox("顯示跨界移動選單", ref C.ShowWorldVisit);
                ImGui.Checkbox("顯示住宅區按鈕", ref C.ShowWards);

                UtilsUI.NextSection();

                ImGui.Checkbox("固定 Lifestream 浮層位置", ref C.FixedPosition);
                if(C.FixedPosition)
                {
                    ImGui.Indent();
                    ImGui.SetNextItemWidth(200f.Scale());
                    ImGuiEx.EnumCombo("水平基準位置", ref C.PosHorizontal);
                    ImGui.SetNextItemWidth(200f.Scale());
                    ImGuiEx.EnumCombo("垂直基準位置", ref C.PosVertical);
                    ImGui.SetNextItemWidth(200f.Scale());
                    ImGui.DragFloat2("偏移", ref C.Offset);

                    ImGui.Unindent();
                }

                UtilsUI.NextSection();

                ImGui.SetNextItemWidth(100f.Scale());
                fixed(int* ptr = &C.ButtonWidthArray[0])
                fixed(byte* sptr = "按鈕左右留白\0"u8)
                {
                    ImGuiNative.InputInt3(sptr, ptr, ImGuiInputTextFlags.None);
                }
                ImGui.SetNextItemWidth(100f.Scale());
                ImGui.InputInt("傳送水晶按鈕上下留白", ref C.ButtonHeightAetheryte);
                ImGui.SetNextItemWidth(100f.Scale());
                ImGui.InputInt("伺服器按鈕上下留白", ref C.ButtonHeightWorld);
                ImGui.Unindent();

                ImGui.Checkbox("按鈕文字靠左對齊", ref C.LeftAlignButtons);
                if(C.LeftAlignButtons)
                {
                    ImGui.SetNextItemWidth(100f);
                    ImGui.DragInt("左側留白（空格數）", ref C.LeftAlignPadding, 0.1f, 0, 20);
                }
            }
        })

        .Section("副本分流切換")
        .Checkbox("啟用", () => ref C.ShowInstanceSwitcher)
        .Checkbox("失敗時重試", () => ref C.InstanceSwitcherRepeat)
        .Checkbox("切換分流前若正在飛行，先降落地面", () => ref C.EnableFlydownInstance)
        .Widget("在伺服器資訊列顯示分流編號", (x) =>
        {
            if(ImGui.Checkbox(x, ref C.EnableDtrBar))
            {
                S.DtrManager.Refresh();
            }
        })
        .SliderInt(150f, "額外按鈕高度", () => ref C.InstanceButtonHeight, 0, 50)
        .Widget("重設分流資料", (x) =>
        {
            if(ImGuiEx.Button(x, C.PublicInstances.Count > 0))
            {
                C.PublicInstances.Clear();
                EzConfig.Save();
            }
        })

        .Section("遊戲視窗整合")
        .Checkbox("下列遊戲視窗開啟時隱藏 Lifestream", () => ref C.HideAddon)
        .If(() => C.HideAddon)
        .Widget(() =>
        {
            if(ImGui.BeginTable("HideAddonTable", 2, ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableSetupColumn("col1", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("col2");

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiEx.SetNextItemFullWidth();
                ImGui.InputTextWithHint("##addnew", "視窗名稱…可用 /xldata ai 查詢", ref AddNew, 100);
                ImGui.TableNextColumn();
                if(ImGuiEx.IconButton(FontAwesomeIcon.Plus))
                {
                    C.HideAddonList.Add(AddNew);
                    AddNew = "";
                }

                List<string> focused = [];
                try
                {
                    foreach(var x in RaptureAtkUnitManager.Instance()->FocusedUnitsList.Entries)
                    {
                        if(x.Value == null) continue;
                        focused.Add(x.Value->NameString);
                    }
                }
                catch(Exception e) { e.Log(); }

                if(focused != null)
                {
                    foreach(var name in focused)
                    {
                        if(name == null) continue;
                        if(C.HideAddonList.Contains(name)) continue;
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGuiEx.TextV(EColor.Green, $"目前焦點：{name}");
                        ImGui.TableNextColumn();
                        ImGui.PushID(name);
                        if(ImGuiEx.IconButton(FontAwesomeIcon.Plus))
                        {
                            C.HideAddonList.Add(name);
                        }
                        ImGui.PopID();
                    }
                }

                ImGui.TableNextRow();
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, 0x88888888);
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, 0x88888888);
                ImGui.TableNextColumn();
                ImGui.Dummy(new Vector2(5f));

                foreach(var s in C.HideAddonList)
                {
                    ImGui.PushID(s);
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiEx.TextV(focused.Contains(s) ? EColor.Green : null, s);
                    ImGui.TableNextColumn();
                    if(ImGuiEx.IconButton(FontAwesomeIcon.Trash))
                    {
                        new TickScheduler(() => C.HideAddonList.Remove(s));
                    }
                    ImGui.PopID();
                }

                ImGui.EndTable();
            }
        })
        .EndIf()
        .Draw();

        if(C.Hidden.Count > 0)
        {
            new NuiBuilder()
            .Section("隱藏的傳送水晶")
            .Widget(() =>
            {
                uint toRem = 0;
                foreach(var x in C.Hidden)
                {
                    ImGuiEx.Text($"{Svc.Data.GetExcelSheet<Aetheryte>().GetRowOrDefault(x)?.AethernetName.ValueNullable?.Name.ToString() ?? x.ToString()}");
                    ImGui.SameLine();
                    if(ImGui.SmallButton($"刪除##{x}"))
                    {
                        toRem = x;
                    }
                }
                if(toRem > 0)
                {
                    C.Hidden.Remove(toRem);
                }
            })
            .Draw();
        }
    }

    private static void DrawExpert()
    {
        new NuiBuilder()
        .Section("進階設定")
        .Widget(() =>
        {
            ImGui.Checkbox("放慢都市傳送網的傳送速度", ref C.SlowTeleport);
            ImGuiEx.HelpMarker("每次都市傳送網傳送時增加指定延遲。");
            if(C.SlowTeleport)
            {
                ImGui.Indent();
                ImGui.SetNextItemWidth(200f.Scale());
                ImGui.DragInt("傳送延遲（毫秒）", ref C.SlowTeleportThrottle);
                ImGui.Unindent();
            }
            ImGuiEx.CheckboxInverted("略過等待遊戲畫面就緒", ref C.WaitForScreenReady);
            ImGuiEx.HelpMarker("啟用後傳送較快，但可能會卡住。");
            ImGui.Checkbox("隱藏進度列", ref C.NoProgressBar);
            ImGuiEx.HelpMarker("隱藏進度列後，將無法從進度列停止 Lifestream 執行工作。");
            ImGuiEx.CheckboxInverted("遠距離執行跨界命令時，不走向附近的傳送水晶", ref C.WalkToAetheryte);
            ImGui.Checkbox("將進度浮層顯示於畫面頂端", ref C.ProgressOverlayToTop);
            ImGui.Checkbox("允許自訂別名及房屋別名覆寫內建指令", ref C.AllowCustomOverrides);
            ImGui.Indent();
            ImGuiEx.TextWrapped(EColor.RedBright, "警告：其他插件可能依賴內建指令。啟用並覆寫指令前，請確認沒有其他插件使用這些指令。");
            ImGui.Unindent();
        })
        .Draw();
    }
}
