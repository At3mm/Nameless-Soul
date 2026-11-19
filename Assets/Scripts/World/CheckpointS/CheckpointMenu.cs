using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace World
{
    [DisallowMultipleComponent]
    public sealed class CheckpointMenu : MonoBehaviour
    {
        [Header("Existing UI pack artwork")]
        public Sprite contentSprite;
        public Sprite[] sectionIcons;
        public Sprite selectionSprite;
        public Font menuFont;
        public Sprite selectionArrow;
        public Color selectionTint = new Color(.72f,.66f,.48f,.28f);
        private readonly List<Button> menuButtons = new List<Button>();
        public Sprite[] statIcons;
        public string checkpointName = "Tượng cổ";
        [Min(0f)] public float closeDuration = .22f;
        [Header("Prayer screen fade")]
        [Min(.05f)] public float prayerFadeOut = .35f;
        [Min(0f)] public float prayerBlackHold = .15f;
        [Min(.05f)] public float prayerFadeIn = .4f;
        private Coroutine prayerRoutine;
        private GameObject prayerOverlay;
        public bool IsPraying => prayerRoutine != null;
        private Coroutine closingRoutine;
        public bool IsClosing => closingRoutine != null;
        private static CheckpointMenu active;
        private static int blockedThroughFrame = -1;
        public static bool AnyOpen => active != null;
        public static bool CanPurifySoulAtShrine=>active!=null&&active.IsOpen&&!active.IsClosing&&!active.IsPraying&&active.CurrentSection==2&&active.player!=null&&!active.player.GetComponent<CharacterHealth>().isDead&&!active.player.IsInCombat;
        public static bool BlocksPauseMenu => AnyOpen || CheckpointService.IsTraveling || Time.frameCount <= blockedThroughFrame;
        public bool IsOpen { get; private set; }
        public int CurrentSection { get; private set; }
        private GameObject view, previousSelection;
        private PlayerInteractor player;
        private Text sectionTitle, description;
        private Image detailIcon;
        private Button prayerButton, travelButton, confirmTravel, backTravel;
        private Button sacrificeButton,purificationButton;
        private Button upgradeButton, confirmUpgrade, backUpgrade;
        private GameObject upgradeView;
        private Text walletLabel, upgradeWallet, upgradeStatus;
        private readonly List<Button> upgradeRows = new List<Button>();
        private int selectedUpgrade;
        private static readonly string[] upgradeNames = { "Máu tối đa", "Stamina tối đa", "Tấn công", "Phòng thủ" };
        public bool IsChoosingUpgrade => upgradeView != null && upgradeView.activeSelf;
        private GameObject travelView;
        private Text travelPage, travelStatus;
        private readonly List<Button> destinationButtons = new List<Button>();
        private List<CheckpointService.Destination> destinations;
        private int selectedDestination;
        private const int DestinationsPerPage = 5;
        public bool IsChoosingDestination => travelView != null && travelView.activeSelf;
        private CanvasGroup hudGroup, menuGroup;
        private float hudAlpha;
        private Texture2D shadeTexture;
        private Sprite shadeSprite;
        private Font font;
        private float previousTimeScale;
        private readonly List<Behaviour> suspended = new List<Behaviour>();
        private readonly List<Image> tabImages = new List<Image>();
        private readonly List<Button> tabButtons = new List<Button>();
        private readonly string[] titles = { "Nâng chỉ số", "Hiến tế vật phẩm", "Thanh tẩy", "Cầu nguyện", "Du hành" };
        private readonly Color ink = new Color(1f, 0.9f, 0.7f);
        private GameObject ownedEventSystem;
        private int openedFrame = -1;
        private float openElapsed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() { active = null; blockedThroughFrame = -1; }

        public bool Open(PlayerInteractor actor)
        {
            if (!isActiveAndEnabled || IsOpen || AnyOpen || InGameCutscene.AnyPlaying || actor == null || !actor.CanUseCheckpoint) return false;
            player = actor; previousTimeScale = Time.timeScale;
            IsOpen = true; active = this; openedFrame = Time.frameCount; MenuSFX.Play(MenuSFX.Kind.Open);
            openElapsed=0;
            AreaNotice.Dismiss();
            Suspend(actor.GetComponent<PlayerController>());
            Suspend(actor.GetComponent<PlayerCombat>());
            Suspend(actor.GetComponent<PlayerDash>());
            Suspend(actor.GetComponent<CharacterParry>());
            Suspend(actor.GetComponent<PlayerHealthPotion>());
            var body = actor.GetComponent<Rigidbody2D>(); if (body != null) body.linearVelocity = Vector2.zero;
            Time.timeScale = 0f;
            if (EventSystem.current == null)
            {
                ownedEventSystem = new GameObject("Checkpoint EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                ownedEventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            var hud = FindFirstObjectByType<PlayerVitalsHUD>();
            if (hud != null && hud.TryGetComponent(out hudGroup)) { hudAlpha = hudGroup.alpha; hudGroup.alpha = 0; }
            BuildView(); SelectSection(0);
            GameEconomy.Changed += RefreshEconomy;
            RefreshEconomy();
            return true;
        }

        private void Suspend(Behaviour control)
        {
            if (control == null || !control.enabled) return;
            suspended.Add(control); control.enabled = false;
        }
        private void Update()
        {
            if (!IsOpen) return;
            if (IsClosing || IsPraying) return;
            openElapsed+=Time.unscaledDeltaTime;
            if (menuGroup != null) menuGroup.alpha = PixelAnimationSettings.Progress(openElapsed,.2f);
            var kb = Keyboard.current;
            if (kb == null) return;
            if (IsChoosingUpgrade)
            {
                if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) SelectUpgrade((selectedUpgrade+3)%4);
                if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) SelectUpgrade((selectedUpgrade+1)%4);
                if (kb.escapeKey.wasPressedThisFrame) CloseUpgradeList();
                else if (kb.eKey.wasPressedThisFrame) ConfirmSelection();
                return;
            }
            if (IsChoosingDestination)
            {
                if (destinations.Count > 0)
                {
                    if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) SelectDestination((selectedDestination + destinations.Count - 1) % destinations.Count);
                    if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) SelectDestination((selectedDestination + 1) % destinations.Count);
                    float scroll = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0;
                    if (scroll != 0) SelectDestination(Mathf.Clamp(selectedDestination + (scroll > 0 ? -1 : 1),0,destinations.Count-1));
                }
                if (kb.escapeKey.wasPressedThisFrame) CloseTravelList();
                else if (kb.eKey.wasPressedThisFrame) ConfirmSelection();
                return;
            }
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) SelectSection((CurrentSection + titles.Length - 1) % titles.Length);
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) SelectSection((CurrentSection + 1) % titles.Length);
            if (kb.escapeKey.wasPressedThisFrame) RequestClose();
            else if (kb.eKey.wasPressedThisFrame) ConfirmSelection();
        }

        private void ConfirmSelection()
        {
            // The interaction press opens the menu; a fresh press confirms its selection.
            if (!IsOpen || IsClosing || Time.frameCount <= openedFrame) return;
            Button action = IsChoosingUpgrade ? confirmUpgrade : IsChoosingDestination ? confirmTravel :
                CurrentSection == 0 ? upgradeButton : CurrentSection == 1 ? sacrificeButton : CurrentSection == 2 ? purificationButton : CurrentSection == 3 ? prayerButton :
                CurrentSection == 4 ? travelButton : null;
            if (action != null && action.isActiveAndEnabled && action.interactable) action.onClick.Invoke();
        }
        private void LateUpdate()
        {
            if (!IsOpen) return;
            var focused = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            foreach (var button in menuButtons)
            {
                if (button == null) continue;
                int tab = tabButtons.IndexOf(button), stat = upgradeRows.IndexOf(button), destination = destinationButtons.IndexOf(button);
                bool selected = tab >= 0 ? tab == CurrentSection : stat >= 0 ? stat == selectedUpgrade :
                    destination >= 0 ? destination == selectedDestination % DestinationsPerPage : button.gameObject == focused;
                var arrow = button.transform.Find("Selection arrow");
                if (arrow != null) arrow.gameObject.SetActive(selected && button.interactable);
            }
        }
        public void RequestClose()
        {
            if (!IsOpen || IsClosing || IsPraying) return;
            if (closeDuration <= 0f) { Close(); return; }
            closingRoutine = StartCoroutine(FadeClosed());
        }
        private System.Collections.IEnumerator FadeClosed()
        {
            menuGroup.interactable = false;
            float start = menuGroup.alpha;
            float elapsed = 0f;
            while (elapsed < closeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f,1f,PixelAnimationSettings.Progress(elapsed,closeDuration));
                menuGroup.alpha = Mathf.Lerp(start,0f,t);
                if (hudGroup != null) hudGroup.alpha = Mathf.Lerp(0f,hudAlpha,t);
                yield return null;
            }
            closingRoutine = null;
            Close();
        }
        public void Close()
        {
            if (prayerRoutine != null) { StopCoroutine(prayerRoutine); prayerRoutine = null; }
            if (prayerOverlay != null) { prayerOverlay.SetActive(false); Destroy(prayerOverlay); prayerOverlay = null; }
            if (closingRoutine != null) { StopCoroutine(closingRoutine); closingRoutine = null; }
            if (!IsOpen) return;
            MenuSFX.Play(MenuSFX.Kind.Close);
            GameEconomy.Changed -= RefreshEconomy;
            blockedThroughFrame = Time.frameCount + 1;
            IsOpen = false;
            if (active == this) active = null;
            Time.timeScale = previousTimeScale;
            foreach (var control in suspended)
            {
                if (control == null) continue;
                var health = control.GetComponent<CharacterHealth>();
                if (health == null || !health.isDead) control.enabled = true;
            }
            suspended.Clear(); tabImages.Clear(); tabButtons.Clear();
            menuButtons.Clear();
            destinationButtons.Clear(); travelView = null; destinations = null;
            upgradeRows.Clear(); upgradeView = null;
            if (hudGroup != null) { hudGroup.alpha = hudAlpha; hudGroup = null; }
            if (view != null) { view.SetActive(false); Destroy(view); }
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy ? previousSelection : null);
            if (ownedEventSystem != null) Destroy(ownedEventSystem);
            if (shadeSprite != null) Destroy(shadeSprite);
            if (shadeTexture != null) Destroy(shadeTexture);
            player = null;
        }

        public void SelectSection(int section)
        {
            if (!IsOpen || IsPraying || section < 0 || section >= titles.Length) return;
            if (IsChoosingDestination) CloseTravelList();
            if (IsChoosingUpgrade) CloseUpgradeList();
            if(CurrentSection != section) MenuSFX.Play(MenuSFX.Kind.Move);
            CurrentSection = section;
            sectionTitle.text = titles[section];
            for (int i = 0; i < tabImages.Count; i++)
            {
                PixelMenuButtonStyle.Select(tabButtons[i],i==section);
                tabButtons[i].GetComponentInChildren<Text>().color = i == section ? new Color(1f,.94f,.8f) : new Color(.78f,.77f,.72f);
            }
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(tabButtons[section].gameObject);
            if (detailIcon != null && sectionIcons != null && section < sectionIcons.Length) detailIcon.sprite = sectionIcons[section];
            if (section == 0)
                description.text = "Dùng EXP để nâng từng thông số.\nChi phí tăng sau mỗi lần nâng.";
            else if (section == 1)
                RefreshSacrifice();
            else if (section == 2)
                RefreshPurification();
            else if (section == 3)
                description.text = "Lưu hành trình tại tượng này và hồi phục.\nQuái thường trở lại; Boss và Elite giữ nguyên.";
            else
                description.text = "Dịch chuyển đến các tượng đã mở.\nTượng đích sẽ là điểm hồi sinh mới.";
            prayerButton.gameObject.SetActive(section == 3);
            sacrificeButton.gameObject.SetActive(section == 1);
            purificationButton.gameObject.SetActive(section == 2);
            travelButton.gameObject.SetActive(section == 4);
            upgradeButton.gameObject.SetActive(section == 0);
            if (section == 0 && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(upgradeButton.gameObject);
            if (section == 1 && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(sacrificeButton.gameObject);
            if (section == 2 && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(purificationButton.gameObject);
            if (section == 3 && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(prayerButton.gameObject);
            if (section == 4 && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(travelButton.gameObject);
        }

        public void OpenUpgradeList()
        {
            if (!IsOpen || CurrentSection != 0 || player == null || player.IsInCombat) return;
            if (upgradeView == null) BuildUpgradeList();
            MenuSFX.Play(MenuSFX.Kind.Confirm);
            upgradeView.SetActive(true); SelectUpgrade(0);
        }
        private void CloseUpgradeList()
        {
            if (IsChoosingUpgrade) MenuSFX.Play(MenuSFX.Kind.Back);
            if (upgradeView != null) upgradeView.SetActive(false);
            blockedThroughFrame = Time.frameCount+1;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(upgradeButton.gameObject);
        }
        private float StatValue(int index)
        {
            var stats = player.GetComponent<CharacterStats>();
            return index == 0 ? stats.maxHP : index == 1 ? stats.maxStamina : index == 2 ? stats.ATK : stats.DEF;
        }
        private void RefreshEconomy()
        {
            if (!IsOpen || player == null) return;
            walletLabel.text = "Gold  " + GameEconomy.Gold + "     •     EXP  " + GameEconomy.Experience;
            for (int i=0;i<4;i++)
            {
                var label = view.transform.Find("Value "+i);
                if (label != null) label.GetComponent<Text>().text = StatValue(i).ToString("0.#");
            }
            if (IsChoosingUpgrade) SelectUpgrade(selectedUpgrade);
            RefreshPotionCount();
            if (CurrentSection == 1) RefreshSacrifice();
            if (CurrentSection == 2) RefreshPurification();
        }
        private bool RejectCombatAction()
        {
            if (!IsOpen || player == null || !player.IsInCombat) return false;
            player.ShowNotice("Không thể dùng tượng khi đang giao tranh.");
            MenuSFX.Play(MenuSFX.Kind.Error);
            return true;
        }
        private void RefreshPurification()
        {
            var settings=SoulConsumption.Settings;int count=GameEconomy.ConsumableCount(ConsumableKind.SoulPurification);
            description.text=(SoulConsumption.Active?"Ăn hồn: Đang nhiễm":"Ăn hồn: Chưa nhiễm")+"\nGây ×"+settings.outgoingDamageMultiplier.ToString("0.##")+" · Nhận ×"+settings.incomingDamageMultiplier.ToString("0.##")+
                "\nSoul/EXP ×"+settings.soulMultiplier.ToString("0.##")+" · Vàng ×"+settings.goldMultiplier.ToString("0.##")+"\nHương thanh hồn: "+count+" · Tỉ lệ chết tới: "+SoulConsumption.NextChancePercent.ToString("0.#")+"%";
            purificationButton.interactable=true;
        }
        public bool PurifySoul()
        {
            if (RejectCombatAction()) return false;
            if(!IsOpen||IsClosing||IsPraying||CurrentSection!=2||player==null||player.GetComponent<CharacterHealth>().isDead||player.IsInCombat)return false;
            if(!GameEconomy.PurifySoulConsumption())
            {player.ShowNotice(!SoulConsumption.Active?"Không có trạng thái Ăn hồn cần thanh tẩy.":"Không có Hương thanh hồn để thanh tẩy.");MenuSFX.Play(MenuSFX.Kind.Error);return false;}
            player.ShowNotice("Đã thanh tẩy Ăn hồn.");
            MenuSFX.Play(MenuSFX.Kind.Confirm);StatusEffectDefaults.EnsureVisuals(player.gameObject,true);
            description.text="Đã thanh tẩy Ăn hồn.\nTỉ lệ nhiễm trở lại "+SoulConsumption.NextChancePercent.ToString("0.#")+"%.\nHương thanh hồn còn: "+GameEconomy.ConsumableCount(ConsumableKind.SoulPurification);
            return true;
        }
        private void RefreshPotionCount()
        {
            var potion = player != null ? player.GetComponent<PlayerHealthPotion>() : null;
            var label = view != null ? view.transform.Find("Value 4") : null;
            if (label != null && potion != null) label.GetComponent<Text>().text = potion.Charges + " / " + potion.capacity;
        }
        private void RefreshSacrifice()
        {
            var potion = player != null ? player.GetComponent<PlayerHealthPotion>() : null;
            bool maximum = potion != null && potion.capacity >= PlayerHealthPotion.MaximumCapacity;
            description.text = LifeSeedItem.Name+" · Có: " + GameEconomy.LifeSeeds + "\n" + (maximum ? "Đã đạt sức chứa tối đa." : "Dâng 1 hạt: sức chứa bình máu +1 vĩnh viễn.") + "\nSức chứa: " + (potion != null ? potion.capacity.ToString() : "—") + " / " + PlayerHealthPotion.MaximumCapacity;
            sacrificeButton.interactable = potion != null;
        }
        public bool Sacrifice()
        {
            if (RejectCombatAction()) return false;
            if (!IsOpen || IsClosing || IsPraying || CurrentSection != 1 || player == null || player.IsInCombat) return false;
            var potion = player.GetComponent<PlayerHealthPotion>();
            if (!GameEconomy.SacrificeLifeSeed(potion))
            { player.ShowNotice(potion!=null && potion.capacity>=PlayerHealthPotion.MaximumCapacity?"Sức chứa bình máu đã đạt tối đa.":GameEconomy.LifeSeeds<=0?"Không đủ Hạt sinh mệnh để hiến tế.":"Chưa thể hiến tế lúc này."); MenuSFX.Play(MenuSFX.Kind.Error); return false; }
            player.ShowNotice("Hiến tế thành công. Sức chứa bình máu +1.");
            potion.AddCharges(1);
            RefreshPotionCount(); RefreshSacrifice();
            MenuSFX.Play(MenuSFX.Kind.Confirm);
            return true;
        }
        public void SelectUpgrade(int index)
        {
            if (!IsChoosingUpgrade) return;
            if(selectedUpgrade != Mathf.Clamp(index,0,3)) MenuSFX.Play(MenuSFX.Kind.Move);
            selectedUpgrade = Mathf.Clamp(index,0,3);
            int price = GameEconomy.NextUpgradeCost;
            upgradeWallet.text = "Gold  " + GameEconomy.Gold + "     •     EXP  " + GameEconomy.Experience;
            for (int i=0;i<upgradeRows.Count;i++)
            {
                float current = StatValue(i), gain = GameEconomy.Settings.Gain((GameEconomy.Stat)i);
                upgradeRows[i].GetComponentInChildren<Text>().text = upgradeNames[i] + "     " + current.ToString("0.#") + "  →  " + (current+gain).ToString("0.#");
                PixelMenuButtonStyle.Select(upgradeRows[i],i==selectedUpgrade);
            }
            upgradeStatus.text = price > 0 ? "Chi phí: " + price + " EXP\n" + (GameEconomy.Experience >= price ? "Nâng một chỉ số sẽ tăng chi phí lần kế tiếp." : "Chưa đủ EXP để nâng chỉ số.") : "Chưa có nâng cấp khả dụng.";
            confirmUpgrade.interactable = true;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(confirmUpgrade.interactable ? confirmUpgrade.gameObject : backUpgrade.gameObject);
        }
        public bool UpgradeSelected()
        {
            if (RejectCombatAction()) return false;
            if (!IsChoosingUpgrade || !IsOpen || CurrentSection != 0 || player == null || player.IsInCombat) return false;
            bool success = GameEconomy.TryUpgrade((GameEconomy.Stat)selectedUpgrade);
            player.ShowNotice(success?"Đã nâng chỉ số.":GameEconomy.NextUpgradeCost<=0?"Chưa có nâng cấp khả dụng.":GameEconomy.Experience<GameEconomy.NextUpgradeCost?"Không đủ Soul để nâng chỉ số.":"Chưa thể nâng chỉ số lúc này.");
            MenuSFX.Play(success ? MenuSFX.Kind.Confirm : MenuSFX.Kind.Error);
            return success;
        }
        private void BuildUpgradeList()
        {
            var overlay = Panel(view.transform,"Stat upgrades",new Vector2(.5f,.5f),Vector2.zero,Vector2.zero,new Color(0,0,0,.78f));
            overlay.rectTransform.anchorMin = Vector2.zero; overlay.rectTransform.anchorMax = Vector2.one;
            upgradeView = overlay.gameObject;
            Panel(upgradeView.transform,"Upgrade backing",new Vector2(.5f,.5f),Vector2.zero,new Vector2(628,528),new Color(.035f,.03f,.02f,.99f));
            var paper = Panel(upgradeView.transform,"Upgrade sheet",new Vector2(.5f,.5f),Vector2.zero,new Vector2(650,550),new Color(.12f,.12f,.10f,.98f));
            ApplyArtwork(paper,contentSprite);
            Label(paper.transform,"Upgrade title","NÂNG CHỈ SỐ",new Vector2(34,-26),new Vector2(590,40),25);
            upgradeWallet = Label(paper.transform,"Upgrade wallet","",new Vector2(36,-76),new Vector2(580,30),19);
            for (int i=0;i<4;i++)
            {
                int index = i;
                var button = MakeButton(paper.transform,"Upgrade "+i,new Vector2(34,-119-i*62),new Vector2(582,56),()=>SelectUpgrade(index));
                button.GetComponentInChildren<Text>().fontSize = 22;
                var nav = button.navigation; nav.mode = Navigation.Mode.None; button.navigation = nav;
                upgradeRows.Add(button);
            }
            upgradeStatus = Label(paper.transform,"Upgrade status","",new Vector2(36,-383),new Vector2(578,52),18);
            Label(paper.transform,"Upgrade controls","↑ ↓ / W S  Chọn chỉ số     •     Mỗi lần nâng dùng EXP",new Vector2(36,-445),new Vector2(578,28),17);
            confirmUpgrade = MakeButton(paper.transform,"Nâng chỉ số  [E]",new Vector2(350,-485),new Vector2(266,38),()=>UpgradeSelected());
            confirmUpgrade.GetComponent<Image>().color = new Color(.4f,.32f,.17f,.95f);
            backUpgrade = MakeButton(paper.transform,"Quay lại  [Esc]",new Vector2(34,-485),new Vector2(266,38),CloseUpgradeList);
            backUpgrade.GetComponent<Image>().color = new Color(.2f,.19f,.15f,.95f);
            foreach (var button in new[] { confirmUpgrade, backUpgrade })
            {
                var nav = button.navigation; nav.mode = Navigation.Mode.None; button.navigation = nav;
                button.GetComponentInChildren<Text>().fontSize = 18;
            }
        }

        public void OpenTravelList()
        {
            if (!IsOpen || IsClosing || IsPraying || CurrentSection != 4 || player == null || player.IsInCombat) return;
            var source = GetComponent<CheckpointInteractable>();
            destinations = CheckpointService.GetTravelDestinations(gameObject.scene.path,source != null ? source.checkpointId : "",true);
            if (travelView == null) BuildTravelList();
            MenuSFX.Play(MenuSFX.Kind.Confirm);
            travelView.SetActive(true);
            int firstAvailable = destinations.FindIndex(destination => !destination.isCurrent);
            SelectDestination(firstAvailable >= 0 ? firstAvailable : 0);
        }

        private void CloseTravelList()
        {
            if (IsChoosingDestination) MenuSFX.Play(MenuSFX.Kind.Back);
            if (travelView != null) travelView.SetActive(false);
            blockedThroughFrame = Time.frameCount + 1;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(travelButton.gameObject);
        }

        public void SelectDestination(int index)
        {
            if (!IsChoosingDestination) return;
            if(destinations.Count > 0 && selectedDestination != Mathf.Clamp(index,0,destinations.Count-1)) MenuSFX.Play(MenuSFX.Kind.Move);
            selectedDestination = destinations.Count > 0 ? Mathf.Clamp(index,0,destinations.Count-1) : 0;
            int start = selectedDestination / DestinationsPerPage * DestinationsPerPage;
            for (int i=0;i<destinationButtons.Count;i++)
            {
                int entry = start+i;
                var button = destinationButtons[i];
                button.gameObject.SetActive(entry < destinations.Count);
                if (entry >= destinations.Count) continue;
                var destination = destinations[entry];
                button.interactable = !destination.isCurrent;
                var rowGroup = button.GetComponent<CanvasGroup>();
                if (rowGroup == null) rowGroup = button.gameObject.AddComponent<CanvasGroup>();
                rowGroup.alpha = destination.isCurrent ? .35f : 1f;
                button.GetComponentInChildren<Text>().text = destination.displayName + "\n" + destination.Area + (destination.isCurrent ? " · Đang ở đây" : "");
                PixelMenuButtonStyle.Select(button,entry==selectedDestination);
            }
            travelPage.text = destinations.Count > 0 ? "↑ ↓ / W S  Chọn tượng     " + (selectedDestination+1) + " / " + destinations.Count : "Chưa mở tượng nào khác để du hành.";
            bool available = destinations.Count > 0 && !destinations[selectedDestination].isCurrent;
            travelStatus.text = destinations.Count > 0 && !available ? "Bạn đang ở khu vực này." : destinations.Count == 0 ? "Chưa có điểm du hành." : "";
            confirmTravel.interactable = available;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(confirmTravel.interactable ? confirmTravel.gameObject : backTravel.gameObject);
        }

        public bool TravelToSelected()
        {
            if (!IsOpen || IsClosing || IsPraying || !IsChoosingDestination || destinations.Count == 0 || player == null) return false;
            if (player.IsInCombat) { travelStatus.text = "Không thể du hành khi đang giao tranh."; player.ShowNotice(travelStatus.text); return false; }
            var destination = destinations[selectedDestination];
            if (destination.isCurrent) { travelStatus.text = "Bạn đang ở khu vực này."; player.ShowNotice(travelStatus.text); return false; }
            if (!CheckpointService.CanTravelTo(destination.scenePath,destination.checkpointId))
            { travelStatus.text = "Chưa thể du hành lúc này."; player.ShowNotice(travelStatus.text); return false; }
            var actor = player;
            Close();
            bool started = CheckpointService.TravelTo(destination.scenePath,destination.checkpointId,actor);
            if (!started) actor.ShowNotice("Không tìm thấy tượng đích hoặc chưa thể du hành.");
            return started;
        }

        private void BuildTravelList()
        {
            var overlay = Panel(view.transform,"Travel destinations",new Vector2(.5f,.5f),Vector2.zero,Vector2.zero,new Color(0,0,0,.78f));
            overlay.rectTransform.anchorMin = Vector2.zero; overlay.rectTransform.anchorMax = Vector2.one;
            travelView = overlay.gameObject;
            Panel(travelView.transform,"Travel backing",new Vector2(.5f,.5f),Vector2.zero,new Vector2(628,528),new Color(.035f,.03f,.02f,.99f));
            var paper = Panel(travelView.transform,"Travel sheet",new Vector2(.5f,.5f),Vector2.zero,new Vector2(650,550),new Color(.12f,.12f,.10f,.98f));
            ApplyArtwork(paper,contentSprite);
            Label(paper.transform,"Travel title","DU HÀNH GIỮA CÁC TƯỢNG",new Vector2(34,-26),new Vector2(590,40),25);
            travelStatus = Label(paper.transform,"Travel status","",new Vector2(36,-76),new Vector2(580,30),17);
            for (int i=0;i<DestinationsPerPage;i++)
            {
                int row = i;
                var button = MakeButton(paper.transform,"Destination "+i,new Vector2(34,-119-i*62),new Vector2(582,56),()=>SelectDestination(selectedDestination / DestinationsPerPage * DestinationsPerPage + row));
                button.GetComponentInChildren<Text>().fontSize = 18;
                var nav = button.navigation; nav.mode = Navigation.Mode.None; button.navigation = nav;
                destinationButtons.Add(button);
            }
            travelPage = Label(paper.transform,"Travel page","",new Vector2(78,-438),new Vector2(490,28),17);
            var previous = MakeButton(paper.transform,"‹",new Vector2(34,-435),new Vector2(32,32),()=>SelectDestination(Mathf.Max(0,selectedDestination-DestinationsPerPage)));
            var next = MakeButton(paper.transform,"›",new Vector2(584,-435),new Vector2(32,32),()=>SelectDestination(Mathf.Min(destinations.Count-1,selectedDestination+DestinationsPerPage)));
            foreach (var pager in new[] { previous,next })
            {
                pager.GetComponent<Image>().color = new Color(.3f,.25f,.15f,.95f);
                var nav = pager.navigation; nav.mode = Navigation.Mode.None; pager.navigation = nav;
            }
            confirmTravel = MakeButton(paper.transform,"Du hành  [E]",new Vector2(350,-485),new Vector2(266,38),()=>TravelToSelected());
            confirmTravel.GetComponent<Image>().color = new Color(.4f,.32f,.17f,.95f);
            var confirmNav = confirmTravel.navigation; confirmNav.mode = Navigation.Mode.None; confirmTravel.navigation = confirmNav;
            var back = MakeButton(paper.transform,"Quay lại  [Esc]",new Vector2(34,-485),new Vector2(266,38),CloseTravelList);
            backTravel = back;
            back.GetComponent<Image>().color = new Color(.2f,.19f,.15f,.95f);
            var backNav = back.navigation; backNav.mode = Navigation.Mode.None; back.navigation = backNav;
        }

        public bool Pray()
        {
            if (RejectCombatAction()) return false;
            if (!IsOpen || IsClosing || IsPraying || CurrentSection != 3 || player == null || player.IsInCombat) return false;
            MenuSFX.Play(MenuSFX.Kind.Confirm);
            prayerRoutine = StartCoroutine(PrayWithFade());
            return true;
        }

        private System.Collections.IEnumerator PrayWithFade()
        {
            prayerOverlay = new GameObject("Prayer screen fade", typeof(RectTransform), typeof(Image));
            prayerOverlay.transform.SetParent(view.transform, false);
            var rect = (RectTransform)prayerOverlay.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = prayerOverlay.GetComponent<Image>();
            image.color = Color.clear; image.raycastTarget = true;
            menuGroup.alpha = 1;
            openElapsed = .2f;
            menuGroup.interactable = false;
            yield return FadePrayer(image, 0, 1, prayerFadeOut);
            if (player != null && !player.GetComponent<CharacterHealth>().isDead && !player.IsInCombat)
                CompletePrayer();
            float hold = 0;
            while (hold < prayerBlackHold) { hold += Time.unscaledDeltaTime; yield return null; }
            yield return FadePrayer(image, 1, 0, prayerFadeIn);
            Destroy(prayerOverlay); prayerOverlay = null;
            menuGroup.interactable = true;
            prayerRoutine = null;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(prayerButton.gameObject);
        }

        private static System.Collections.IEnumerator FadePrayer(Image image, float from, float to, float duration)
        {
            float elapsed = 0; duration = Mathf.Max(.05f, duration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                image.color = new Color(0, 0, 0, Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, PixelAnimationSettings.Progress(elapsed,duration))));
                yield return null;
            }
            image.color = new Color(0, 0, 0, to);
        }

        private void CompletePrayer()
        {
            var checkpoint = GetComponent<CheckpointInteractable>();
            if (checkpoint == null || !CheckpointService.Save(gameObject.scene.path, checkpoint.checkpointId, checkpoint.RespawnPosition, checkpointName, true))
            { description.text = "Không thể lưu hành trình tại tượng này."; player.ShowNotice(description.text); return; }
            GameEconomy.SaveProgress();
            NormalEnemyRespawns.ResetNormals(gameObject.scene);
            CheckpointInteractable.RestorePlayer(player);
            var potion = player.GetComponent<PlayerHealthPotion>();
            var value = view.transform.Find("Value 4");
            if (value != null && potion != null) value.GetComponent<Text>().text = potion.Charges + " / " + potion.capacity;
            description.text = "Đã lưu hành trình và hồi phục đầy đủ.\nTiếp tục chơi sẽ trở lại tượng này.";
        }

        private void BuildView()
        {
            font = menuFont != null ? menuFont : GameFont.Current;
            view = new GameObject("Checkpoint Menu", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            view.transform.SetParent(transform, false);
            view.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            view.GetComponent<Canvas>().sortingOrder = 2500;
            menuGroup = view.GetComponent<CanvasGroup>(); menuGroup.alpha = 0;
            var scaler = view.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280,720); scaler.matchWidthOrHeight = .5f;
            var dim = Panel(view.transform,"Dim",new Vector2(.5f,.5f),Vector2.zero,Vector2.zero,new Color(0,0,0,.18f));
            dim.rectTransform.anchorMin = Vector2.zero; dim.rectTransform.anchorMax = Vector2.one;
            // A soft left veil leaves the checkpoint visible between the two UI columns.
            shadeTexture = new Texture2D(128,1,TextureFormat.RGBA32,false);
            shadeTexture.wrapMode = TextureWrapMode.Clamp;
            for(int i=0;i<128;i++) shadeTexture.SetPixel(i,0,new Color(.025f,.027f,.025f,Mathf.Lerp(.97f,0,Mathf.SmoothStep(0,1,i/127f))));
            shadeTexture.Apply();
            shadeSprite = Sprite.Create(shadeTexture,new Rect(0,0,128,1),new Vector2(.5f,.5f),100);
            var veil = Panel(view.transform,"Left veil",new Vector2(0,.5f),new Vector2(310,0),new Vector2(620,720),Color.white);
            veil.sprite = shadeSprite;
            Label(view.transform,"Title",checkpointName,new Vector2(64,-64),new Vector2(400,46),30);
            Rule(view.transform,new Vector2(62,-119),new Vector2(382,1));
            Label(view.transform,"Saved","CẦU NGUYỆN ĐỂ LƯU HÀNH TRÌNH",new Vector2(66,-132),new Vector2(350,26),14).color = new Color(.60f,.73f,.70f);
            for(int i=0;i<titles.Length;i++)
            {
                int index = i;
                var button = MakeButton(view.transform,titles[i],new Vector2(54,-184-i*66),new Vector2(386,48),()=>SelectSection(index));
                tabButtons.Add(button); tabImages.Add(button.GetComponent<Image>());
                var image = button.GetComponent<Image>(); image.sprite = selectionSprite; image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 2.5f;
                var nav = button.navigation; nav.mode = Navigation.Mode.None; button.navigation = nav;
                button.transition = Selectable.Transition.None;
                var trigger = button.gameObject.AddComponent<EventTrigger>();
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                entry.callback.AddListener(_=>SelectSection(index)); trigger.triggers.Add(entry);
                var label = button.GetComponentInChildren<Text>(); label.alignment = TextAnchor.MiddleLeft;
                label.rectTransform.anchoredPosition = new Vector2(62,0); label.rectTransform.sizeDelta = new Vector2(304,48); label.fontSize = 23;
                if(sectionIcons != null && i<sectionIcons.Length) Icon(button.transform,"Section icon",sectionIcons[i],new Vector2(31,-24),30);
                Rule(view.transform,new Vector2(70,-238-i*66),new Vector2(340,1));
            }
            var sheet = Panel(view.transform,"Information sheet",new Vector2(0,1),new Vector2(1000,-292),new Vector2(446,452),new Color(.10f,.11f,.12f,.82f));
            ApplyArtwork(sheet,contentSprite);
            var top = new Vector2(800,-91);
            walletLabel = Label(view.transform,"Wallet","",new Vector2(800,-55),new Vector2(410,30),18);
            Label(view.transform,"Info heading","TRẠNG THÁI HIỆN TẠI",top,new Vector2(378,30),16).color = new Color(.73f,.71f,.65f);
            var stats = player.GetComponent<CharacterStats>(); var potion = player.GetComponent<PlayerHealthPotion>();
            string[] names = {"Máu tối đa","Stamina tối đa","Tấn công","Phòng thủ","Bình máu"};
            string[] values = {stats.maxHP.ToString("0.#"),stats.maxStamina.ToString("0.#"),stats.ATK.ToString("0.#"),stats.DEF.ToString("0.#"),potion!=null?potion.Charges+" / "+potion.capacity:"—"};
            for(int i=0;i<names.Length;i++)
            {
                float y = -137-i*37;
                if(statIcons != null && i<statIcons.Length) Icon(view.transform,"Stat icon "+i,statIcons[i],new Vector2(818,y-13),24);
                Label(view.transform,"Stat "+i,names[i],new Vector2(843,y),new Vector2(245,28),20).color = new Color(.85f,.83f,.77f);
                Label(view.transform,"Value "+i,values[i],new Vector2(1085,y),new Vector2(100,28),20).alignment = TextAnchor.MiddleRight;
            }
            Rule(view.transform,new Vector2(800,-333),new Vector2(388,1));
            detailIcon = Icon(view.transform,"Detail icon",sectionIcons!=null && sectionIcons.Length>0?sectionIcons[0]:null,new Vector2(823,-367),34);
            sectionTitle = Label(view.transform,"Section title","",new Vector2(855,-350),new Vector2(330,38),23);
            description = Label(view.transform,"Description","",new Vector2(805,-405),new Vector2(384,118),18);
            description.color = new Color(.77f,.76f,.71f);
            prayerButton = MakeButton(view.transform,"Cầu nguyện  [E]",new Vector2(842,-476),new Vector2(280,34),()=>Pray());
            prayerButton.gameObject.name = "PerformPrayer";
            prayerButton.GetComponent<Image>().color = new Color(.35f,.29f,.17f,.95f);
            prayerButton.GetComponentInChildren<Text>().fontSize = 18;
            var prayerNav = prayerButton.navigation; prayerNav.mode = Navigation.Mode.None; prayerButton.navigation = prayerNav;
            prayerButton.gameObject.SetActive(false);
            sacrificeButton = MakeButton(view.transform,"Hiến tế 1 hạt  [E]",new Vector2(842,-510),new Vector2(280,34),()=>Sacrifice());
            sacrificeButton.gameObject.name = "PerformSacrifice";
            sacrificeButton.GetComponent<Image>().color = new Color(.35f,.29f,.17f,.95f);
            sacrificeButton.GetComponentInChildren<Text>().fontSize = 18;
            var sacrificeNav = sacrificeButton.navigation; sacrificeNav.mode = Navigation.Mode.None; sacrificeButton.navigation = sacrificeNav;
            sacrificeButton.gameObject.SetActive(false);
            purificationButton=MakeButton(view.transform,"Dùng 1 Hương thanh hồn  [E]",new Vector2(812,-510),new Vector2(340,34),()=>PurifySoul());
            purificationButton.gameObject.name="PerformPurification";purificationButton.GetComponent<Image>().color=new Color(.35f,.29f,.17f,.95f);purificationButton.GetComponentInChildren<Text>().fontSize=17;
            var purificationNav=purificationButton.navigation;purificationNav.mode=Navigation.Mode.None;purificationButton.navigation=purificationNav;purificationButton.gameObject.SetActive(false);
            travelButton = MakeButton(view.transform,"Chọn điểm đến  [E]",new Vector2(842,-476),new Vector2(280,34),OpenTravelList);
            travelButton.gameObject.name = "OpenTravel";
            travelButton.GetComponent<Image>().color = new Color(.35f,.29f,.17f,.95f);
            travelButton.GetComponentInChildren<Text>().fontSize = 18;
            var travelNav = travelButton.navigation; travelNav.mode = Navigation.Mode.None; travelButton.navigation = travelNav;
            travelButton.gameObject.SetActive(false);
            upgradeButton = MakeButton(view.transform,"Chọn chỉ số  [E]",new Vector2(842,-476),new Vector2(280,34),OpenUpgradeList);
            upgradeButton.gameObject.name = "OpenUpgrades";
            upgradeButton.GetComponent<Image>().color = new Color(.35f,.29f,.17f,.95f);
            upgradeButton.GetComponentInChildren<Text>().fontSize = 18;
            var upgradeNav = upgradeButton.navigation; upgradeNav.mode = Navigation.Mode.None; upgradeButton.navigation = upgradeNav;
            upgradeButton.gameObject.SetActive(false);
            Rule(view.transform,new Vector2(63,-609),new Vector2(390,1));
            Label(view.transform,"Controls","↑ ↓ / W S  Chọn mục   •   E  Xác nhận   •   Esc  Rời tượng",new Vector2(66,-666),new Vector2(700,30),16);
            var close = MakeButton(view.transform,"Rời tượng  [Esc]",new Vector2(1030,-643),new Vector2(180,38),RequestClose);
            close.GetComponent<Image>().color = new Color(.12f,.12f,.11f,.8f);
            close.GetComponentInChildren<Text>().fontSize = 17;
        }

        private void Rule(Transform parent, Vector2 pos, Vector2 size)
        {
            Panel(parent,"Rule",new Vector2(0,1),pos+new Vector2(size.x/2,-size.y/2),size,new Color(.58f,.54f,.43f,.45f)).raycastTarget = false;
        }
        private Image Icon(Transform parent,string name,Sprite sprite,Vector2 pos,float size)
        {
            var image = Panel(parent,name,new Vector2(0,1),pos,new Vector2(size,size),Color.white);
            image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
            if(sprite==null) image.enabled = false;
            return image;
        }
        private Image Panel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor; rect.anchoredPosition = pos; rect.sizeDelta = size;
            var image = go.GetComponent<Image>(); image.color = color; return image;
        }
        private Text Label(Transform parent, string name, string text, Vector2 pos, Vector2 size, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0,1); rect.pivot = new Vector2(0,1); rect.anchoredPosition = pos; rect.sizeDelta = size;
            var label = go.GetComponent<Text>(); label.font = font; label.fontSize = fontSize; label.text = text; label.color = ink; label.raycastTarget = false;
            return label;
        }
        private Button MakeButton(Transform parent, string text, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var image = Panel(parent, text, new Vector2(0,1), pos + new Vector2(size.x*.5f,-size.y*.5f), size, Color.white);
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
            menuButtons.Add(button);
            var colors = button.colors; colors.highlightedColor = new Color(1f,.97f,.88f); colors.selectedColor = colors.highlightedColor; colors.pressedColor = new Color(.85f,.8f,.65f); button.colors = colors;
            var arrow = Icon(image.transform,"Selection arrow",selectionArrow,new Vector2(-12,-size.y*.5f),16);
            arrow.rectTransform.localScale = new Vector3(-1,1,1);
            arrow.gameObject.SetActive(false);
            var label = Label(image.transform, "Label", text, Vector2.zero, size, 21); label.alignment = TextAnchor.MiddleCenter;
            PixelMenuButtonStyle.Apply(button);
            return button;
        }
        private static void ApplyArtwork(Image image, Sprite sprite)
        {
            if (sprite == null) return;
            image.sprite = sprite; image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
        }
        private void OnDisable() { Close(); }
    }
}

