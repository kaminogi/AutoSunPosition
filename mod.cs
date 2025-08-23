// mod.cs - Auto Sun Position (Theme Mixer 2.5 & Play It セーフ / EndOfFrame パッチ内蔵)
using System;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.UI;
using ColossalFramework.Plugins;
using ICities;
using UnityEngine;
using System.IO;

namespace AutoSunPosition
{
    public sealed class Mod : IUserMod
    {
        public string Name => I18n.T("mod.name", "Auto Sun Position");
        public string Description => I18n.T("mod.desc", "Auto-rotate sun azimuth to avoid backlighting.");



        private const string SettingsFile = "AutoSunPositionSettings";
        internal static readonly SavedBool SavedManageLongitude =new SavedBool("ManageLongitude", SettingsFile, true, true);
        internal static readonly SavedBool SavedRenderItSafe = new SavedBool("RenderItSafe", SettingsFile, true, true);
        internal static readonly SavedFloat SavedLongitude = new SavedFloat("LongitudeDeg", SettingsFile, 0f, true);
        internal static readonly SavedBool SavedAutoFollow = new SavedBool("AutoFollow", SettingsFile, true, true);
        internal static readonly SavedFloat SavedBacklightThreshold = new SavedFloat("BacklightThresholdDeg", SettingsFile, 60f, true);
        internal static readonly SavedFloat SavedPanelX = new SavedFloat("PanelX", SettingsFile, 20f, true);
        internal static readonly SavedFloat SavedPanelY = new SavedFloat("PanelY", SettingsFile, 100f, true);
        internal static readonly SavedFloat SavedSmoothingSec = new SavedFloat("SmoothingSeconds", SettingsFile, 1.0f, true);
        internal static readonly SavedFloat SavedCheckIntervalSec = new SavedFloat("CheckIntervalSeconds", SettingsFile, 1.0f, true);
        internal static readonly SavedFloat SavedAzimuthOffsetDeg = new SavedFloat("AzimuthOffsetDeg", SettingsFile, 0f, true);
        internal static readonly SavedFloat SavedButtonX = new SavedFloat("ButtonX", SettingsFile, 60f, true);
        internal static readonly SavedFloat SavedButtonY = new SavedFloat("ButtonY", SettingsFile, 240f, true);
        internal static readonly SavedBool SavedPanelVisible = new SavedBool("PanelVisible", SettingsFile, true, true);

        public void OnSettingsUI(UIHelperBase helper)
        {
            var h = helper as UIHelper;
            if (h == null) return;
            var group = h.AddGroup("Auto Sun Position") as UIHelper;

            var chk = h.AddCheckbox(
                I18n.T("settings.safe"),
                SavedRenderItSafe.value,
                v => SavedRenderItSafe.value = v
            ) as UICheckBox;
            if (chk != null) chk.tooltip = I18n.T("settings.safe.tip");

            CreateSliderWithValue(group, I18n.T("settings.th"),
                5f, 180f, 1f, SavedBacklightThreshold.value,
                "{0:0}", I18n.T("settings.th.tip"),
                v => SavedBacklightThreshold.value = v);

            CreateSliderWithValue(group, I18n.T("settings.smooth"),
                0.1f, 2.0f, 0.1f, SavedSmoothingSec.value,
                "{0:0.0}s", I18n.T("settings.smooth.tip"),
                v => SavedSmoothingSec.value = v);

            CreateSliderWithValue(group, I18n.T("settings.interval"),
                0.25f, 3.0f, 0.25f, SavedCheckIntervalSec.value,
                "{0:0.00}s", I18n.T("settings.interval.tip"),
                v => SavedCheckIntervalSec.value = v);

            CreateSliderWithValue( group,  I18n.T("settings.offset", "Sun azimuth offset (°)"),
                -90f, 90f, 1f,
                SavedAzimuthOffsetDeg.value,
                "{0:0}°",
                I18n.T("settings.offset.tip", "Rotate target by this angle. Positive = clockwise."),
                v => SavedAzimuthOffsetDeg.value = v
            );

        }

        /// <summary>
        /// 設定タブ用：スライダー + 現在値ラベル を並べて作る簡易ヘルパー
        /// </summary>
        private static UISlider CreateSliderWithValue(
            UIHelperBase groupBase,
            string label, float min, float max, float step, float initial,
            string valueFmt, string tooltip,
            Action<float> onChanged)
        {
            UISlider slider = groupBase.AddSlider(label, min, max, step, initial, v =>
            {
                onChanged?.Invoke(v);
            }) as UISlider;

            var uiHelper = groupBase as UIHelper;
            UIComponent uiComp = uiHelper != null ? uiHelper.self as UIComponent : null;

            if (slider != null)
            {
                slider.tooltip = tooltip;
                slider.width = 320f;
                if (!string.IsNullOrEmpty(slider.backgroundSprite))
                {
                }
                else
                {
                    slider.backgroundSprite = "ScrollbarTrack";
                }

                if (slider.thumbObject == null)
                {
                    var thumb = slider.AddUIComponent<UISlicedSprite>();
                    thumb.name = "Thumb";
                    thumb.spriteName = "ScrollbarThumb";
                    thumb.size = new Vector2(12f, 20f);
                    thumb.relativePosition = new Vector3(0f, -6f);
                    slider.thumbObject = thumb;
                }
            }

            if (uiComp != null && slider != null)
            {
                UILabel val = uiComp.AddUIComponent<UILabel>();
                val.text = string.Format(valueFmt, initial);
                val.tooltip = tooltip;

                val.relativePosition = new Vector3(slider.relativePosition.x + slider.width + 12f,
                                                   slider.relativePosition.y - 2f);

                slider.eventValueChanged += (_, v) =>
                {
                    if (val != null) val.text = string.Format(valueFmt, v);
                };

            }

            return slider; 
        }


    }

    public sealed class Loading : LoadingExtensionBase
    {
        private GameObject _runner;
        private GameObject _eofPatcher;

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);

            if (_runner == null)
            {
                _runner = new GameObject("AutoSunPositionRunner");
                _runner.AddComponent<AutoSunController>();
                UnityEngine.Object.DontDestroyOnLoad(_runner);
            }

            if (_eofPatcher == null)
            {
                _eofPatcher = new GameObject("AutoSun_EndOfFramePatcher");
                var p = _eofPatcher.AddComponent<AutoSunEndFramePatcher>();
                p.enabled = Mod.SavedRenderItSafe.value;
                UnityEngine.Object.DontDestroyOnLoad(_eofPatcher);
            }
        }

        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();
            if (_runner != null) { UnityEngine.Object.Destroy(_runner); _runner = null; }
            if (_eofPatcher != null) { UnityEngine.Object.Destroy(_eofPatcher); _eofPatcher = null; }
        }
    }

    internal sealed class AutoSunController : MonoBehaviour
    {
        // UI
        private UIPanel _panel;
        private UISlider _slider;
        private UILabel _valueLabel;
        private UICheckBox _checkAuto;
        private UIButton _toggleBtn;

        // Sun control
        private Light _sun;
        private float _currentAzimuthDeg;   // -180..180
        private float _targetAzimuthDeg;
        private float _elevationRad;

        // Tween
        private float _tweenStartDeg;
        private float _tweenStartTime;
        private float _tweenDuration;

        // Auto
        private float _lastCheck;
        private float _checkInterval;
        private float _backlightThresholdDeg;

        // Bridges
        private ThemeMixerBridge _tm;

        private Camera CurrentCamera
        {
            get
            {
                var cam = Camera.main;
                if (cam != null) return cam;

                try
                {
                    var cc = ToolsModifierControl.cameraController;
                    if (cc != null)
                    {
                        const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
                        var t = typeof(CameraController);

                        var f1 = t.GetField("m_currentCamera", F);
                        if (f1 != null) { var c1 = f1.GetValue(cc) as Camera; if (c1 != null) return c1; }

                        var f2 = t.GetField("m_camera", F);
                        if (f2 != null) { var c2 = f2.GetValue(cc) as Camera; if (c2 != null) return c2; }

                        var p1 = t.GetProperty("currentCamera", F);
                        if (p1 != null) { var c3 = p1.GetValue(cc, null) as Camera; if (c3 != null) return c3; }
                    }
                }
                catch { }

                return Camera.current ?? cam;
            }
        }

        // modのパス取得
        private static string GetModRootPath()
        {
            try
            {
                var selfAsm = typeof(Mod).Assembly;
                foreach (var pi in PluginManager.instance.GetPluginsInfo())
                {
                    try
                    {
                        var asms = pi.GetAssemblies();
                        if (asms == null) continue;
                        foreach (var a in asms)
                            if (ReferenceEquals(a, selfAsm))
                                return pi.modPath; 
                    }
                    catch {  }
                }
            }
            catch {  }

            // フォールバック（Location → CodeBase）
            try
            {
                var loc = typeof(Mod).Assembly.Location;
                if (!string.IsNullOrEmpty(loc))
                    return System.IO.Path.GetDirectoryName(loc);
            }
            catch { }

            try
            {
                var cb = typeof(Mod).Assembly.CodeBase; // file:///...
                if (!string.IsNullOrEmpty(cb))
                    return System.IO.Path.GetDirectoryName(new System.Uri(cb).LocalPath);
            }
            catch { }

            return null; // 最終手段：呼び出し側で null チェック
        }

        private void CreateToggleButton() {
            var view = UIView.GetAView();
            _toggleBtn = (UIButton)view.AddUIComponent(typeof(UIButton));

            _toggleBtn.size = new Vector2(36f, 36f);
            _toggleBtn.relativePosition = new Vector3(Mod.SavedButtonX.value, Mod.SavedButtonY.value);
            _toggleBtn.tooltip = I18n.T("panel.toggle.tip", "Toggle Auto Sun UI");


            // ここは CreateToggleButton() の try ブロック内（パス組み立て部分を置き換え）
            string modRoot = GetModRootPath();
            if (string.IsNullOrEmpty(modRoot))
                throw new System.Exception("modRoot not found");

            string resDir = System.IO.Path.Combine(modRoot, "Resources");
            string icon = System.IO.Path.Combine(resDir, "autosun_icon.png");
            string iconH = System.IO.Path.Combine(resDir, "autosun_icon_hover.png");

            //アイコン画像の有無確認（デバッグ用）
            //UnityEngine.Debug.Log("[AutoSun] modRoot=" + modRoot);
            //UnityEngine.Debug.Log($"[AutoSun] icon: {icon}  exists={System.IO.File.Exists(icon)}");
            //UnityEngine.Debug.Log($"[AutoSun] iconH: {iconH} exists={System.IO.File.Exists(iconH)}");

            // 2枚からアトラス生成（2枚の画像を扱うよりアトラスにしたほうが処理が軽くなる）
            var atlas = IconAtlasLoader.CreateAtlasFromFiles(
                "AutoSunIconAtlas",
                new[] { icon, System.IO.File.Exists(iconH) ? iconH : null },
                new[] { "autosun", "autosun_hover" }
            );

            if (atlas != null)
            {
                _toggleBtn.atlas = atlas;
                _toggleBtn.normalBgSprite = "autosun";
                _toggleBtn.hoveredBgSprite = (atlas["autosun_hover"] != null) ? "autosun_hover" : "autosun";
                _toggleBtn.pressedBgSprite = _toggleBtn.hoveredBgSprite;
                _toggleBtn.disabledBgSprite = "autosun";
            }
            else
            {
                UnityEngine.Debug.LogWarning("[AutoSun] icon atlas is null; falling back to vanilla icon");
                _toggleBtn.normalBgSprite = "ToolbarIconZoomOutCity";
            }



            // クリックでパネル表示/非表示
            _toggleBtn.eventClicked += (_, __) =>
            {
                if (_panel == null) return;
                _panel.isVisible = !_panel.isVisible;
                Mod.SavedPanelVisible.value = _panel.isVisible;
            };

            // 左ドラッグで位置移動
            _toggleBtn.eventMouseMove += (c, p) =>
            {
                if ((p.buttons & UIMouseButton.Left) == 0) return;
                var rp = _toggleBtn.relativePosition;
                _toggleBtn.relativePosition = new Vector3(rp.x + p.moveDelta.x, rp.y - p.moveDelta.y);
                Mod.SavedButtonX.value = _toggleBtn.relativePosition.x;
                Mod.SavedButtonY.value = _toggleBtn.relativePosition.y;
            };
        }
        internal static class IconAtlasLoader
        {
            // files[]: pngのフルパス。null可（無い場合は飛ばす）
            // names[]: スプライト名（filesと同じ長さ）
            public static UITextureAtlas CreateAtlasFromFiles(string atlasName, string[] files, string[] names)
            {
                try
                {
                    var view = UIView.GetAView();
                    var baseMat = UnityEngine.Object.Instantiate(view.defaultAtlas.material);
                    baseMat.name = atlasName + "_Mat";

                    // 有効な画像だけ読み込み
                    var texList = new System.Collections.Generic.List<Texture2D>();
                    var nameList = new System.Collections.Generic.List<string>();

                    for (int i = 0; i < files.Length; i++)
                    {
                        var path = files[i];
                        if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) continue;

                        byte[] bytes = System.IO.File.ReadAllBytes(path);
                        var tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                        if (!tex.LoadImage(bytes)) continue;
                        tex.name = (names != null && i < names.Length && !string.IsNullOrEmpty(names[i]))
                                   ? names[i] : System.IO.Path.GetFileNameWithoutExtension(path);

                        // 透過が荒い場合はフィルターをオフに
                        tex.filterMode = FilterMode.Bilinear;

                        texList.Add(tex);
                        nameList.Add(tex.name);
                    }

                    if (texList.Count == 0) return null;

                    // 1枚のテクスチャにパック
                    var atlasTex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                    var rects = atlasTex.PackTextures(texList.ToArray(), 2, 1024); // 余白2px, 最大1024

                    var atlas = ScriptableObject.CreateInstance<UITextureAtlas>();
                    atlas.name = atlasName;
                    atlas.material = baseMat;
                    atlas.material.mainTexture = atlasTex;

                    for (int i = 0; i < texList.Count; i++)
                    {
                        var si = new UITextureAtlas.SpriteInfo
                        {
                            name = nameList[i],
                            texture = atlasTex,
                            region = rects[i],          // PackTexturesが返したUV
                            border = new RectOffset()
                        };
                        atlas.AddSprite(si);
                    }

                    UnityEngine.Debug.Log($"[AutoSun] atlas '{atlasName}' created with {texList.Count} sprite(s).");
                    return atlas;
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.Log($"[AutoSun] CreateAtlasFromFiles failed: {e.Message}");
                    return null;
                }
            }
        }

        public void Awake()
        {
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            _tweenDuration = Mathf.Max(0.1f, Mod.SavedSmoothingSec.value);
            _checkInterval = Mathf.Max(0.1f, Mod.SavedCheckIntervalSec.value);
            _backlightThresholdDeg = Mathf.Clamp(Mod.SavedBacklightThreshold.value, 1f, 179f);

            _tm = new ThemeMixerBridge();

            FindSunLight();
            ReadSunAnglesFromLight();

            _targetAzimuthDeg = _currentAzimuthDeg = Normalize180(Mod.SavedLongitude.value);
            ApplyAzimuthImmediate(_currentAzimuthDeg, false);
            BuildUI();

            var patcher = GameObject.FindObjectOfType<AutoSunEndFramePatcher>();
            if (patcher != null) patcher.SetThemeMixerBridge(_tm);
            SetControlsEnabled(Mod.SavedManageLongitude.value);
            CreateToggleButton();
            if (_panel != null) _panel.isVisible = Mod.SavedPanelVisible.value;
        }

        public void LateUpdate()
        {
            if (!Mod.SavedManageLongitude.value) return;
            if (_sun == null)
            {
                FindSunLight();
                if (_sun == null) return;
                ReadSunAnglesFromLight();
            }

            // Tween
            if (_tweenDuration > 0f)
            {
                float t = Mathf.Clamp01((Time.time - _tweenStartTime) / _tweenDuration);
                t = t * t * (3f - 2f * t); // 光源移動のイージング（きもちよくなる）
                float lerped = Mathf.LerpAngle(_tweenStartDeg, _targetAzimuthDeg, t);
                SetSunAzimuth(lerped, true, true, false);

                if (Mod.SavedRenderItSafe.value)
                {
                    var rot = _sun.transform.rotation;
                    StartCoroutine(ApplySunRotationAtEndOfFrame(rot));
                }
            }

            // Auto follow
            if (Mod.SavedAutoFollow.value && Time.time - _lastCheck >= _checkInterval)
            {
                _lastCheck = Time.time;
                TryAutoFollow();
            }
        }

        // 置き換え後：水平判定版（前方キープ寄り）
        private void TryAutoFollow()
        {
            var cam = CurrentCamera;
            if (cam == null) return;
            if (!Mod.SavedManageLongitude.value) return;

            Vector3 camFwd = cam.transform.forward.normalized;
            Vector3 sunDir = GetSunDirection();                 // 光が「進む」向き（＝シーン奥へ）
                                                                // ※順光＝Angle(sunDir, camFwd) が大きい / 逆光＝小さい

            // --- 3D判定：角度が閾値以下なら“逆光” ---
            float ang3D = Vector3.Angle(sunDir, camFwd);
            bool backlit3D = ang3D <= _backlightThresholdDeg;

            // --- 水平判定：方位差が閾値以下なら“逆光” ---
            Vector3 camH = new Vector3(camFwd.x, 0f, camFwd.z);
            Vector3 sunH = new Vector3(sunDir.x, 0f, sunDir.z);
            if (camH.sqrMagnitude < 1e-6f || sunH.sqrMagnitude < 1e-6f) return;
            camH.Normalize(); sunH.Normalize();

            float camAz = Mathf.Atan2(camH.x, camH.z) * Mathf.Rad2Deg;
            float sunAz = Mathf.Atan2(sunH.x, sunH.z) * Mathf.Rad2Deg;
            float azDiff = Mathf.Abs(Mathf.DeltaAngle(camAz, sunAz));   // 0=ほぼ同じ方位＝逆光寄り
            bool backlitHoriz = azDiff <= _backlightThresholdDeg;

            float offset = Mathf.Clamp(Mod.SavedAzimuthOffsetDeg.value, -90f, 90f);
            float targetAz = Normalize180(camAz + offset);
            if (backlit3D || backlitHoriz)
            {
                StartTweenTo(targetAz + 180f);
            }
        }



        #region Sun helpers

        private void FindSunLight()
        {
            var byName = GameObject.Find("Sun") ?? GameObject.Find("Directional Light");
            if (byName != null)
            {
                var li = byName.GetComponent<Light>();
                if (li != null && li.type == LightType.Directional) { _sun = li; return; }
            }

            Light strongest = null;
            float intensity = -1f;
            var all = GameObject.FindObjectsOfType<Light>();
            for (int i = 0; i < all.Length; i++)
            {
                var li = all[i];
                if (li == null || li.type != LightType.Directional) continue;
                if (li.intensity > intensity) { intensity = li.intensity; strongest = li; }
            }
            _sun = strongest;
        }

        private void ReadSunAnglesFromLight()
        {
            if (_sun == null) return;
            Vector3 d = GetSunDirection();
            _elevationRad = Mathf.Asin(Mathf.Clamp(d.y, -1f, 1f));
            _currentAzimuthDeg = AzimuthDegFromDir(new Vector3(d.x, 0f, d.z).normalized);
        }

        private Vector3 GetSunDirection() => -this._sun.transform.forward.normalized;

        private static float AzimuthDegFromDir(Vector3 horizDir)
        {
            float deg = Mathf.Atan2(horizDir.x, horizDir.z) * Mathf.Rad2Deg;
            return Normalize180(deg);
        }

        private static float Normalize180(float deg)
        {
            while (deg > 180f) deg -= 360f;
            while (deg < -180f) deg += 360f;
            return deg;
        }

        private void SetSunAzimuth(float azimuthDeg, bool pushToBridges, bool updateSlider, bool save)
        {
            if (_sun == null) return;

            azimuthDeg = Normalize180(azimuthDeg);
            _currentAzimuthDeg = azimuthDeg;

            float az = azimuthDeg * Mathf.Deg2Rad;
            float cosE = Mathf.Cos(_elevationRad);
            Vector3 newDir = new Vector3(Mathf.Sin(az) * cosE, Mathf.Sin(_elevationRad), Mathf.Cos(az) * cosE);
            _sun.transform.rotation = Quaternion.LookRotation(-newDir, Vector3.up);

            if (updateSlider && _slider != null)
            {
                _slider.eventValueChanged -= OnSliderChanged;
                _slider.value = _currentAzimuthDeg;
                if (_valueLabel != null) _valueLabel.text = $"{_currentAzimuthDeg:0.0}°";
                _slider.eventValueChanged += OnSliderChanged;
            }

            if (pushToBridges) { if (_tm != null) _tm.TryWriteLongitude(_currentAzimuthDeg); }

            if (save) Mod.SavedLongitude.value = _currentAzimuthDeg;
        }

        private void ApplyAzimuthImmediate(float deg, bool updateStorage)
        {
            _tweenDuration = Mathf.Max(0.1f, Mod.SavedSmoothingSec.value);
            SetSunAzimuth(deg, true, true, updateStorage);
        }

        // スムーズにカメラを移動（逆光判定の結果ギリギリ移動対象でも、0.1度未満なら移動しない）
        private void StartTweenTo(float newAz)
        {
            newAz = Normalize180(newAz);
            if (Mathf.Abs(Mathf.DeltaAngle(_currentAzimuthDeg, newAz)) < 0.1f) return;
            _targetAzimuthDeg = newAz; _tweenStartDeg = _currentAzimuthDeg;
            _tweenStartTime = Time.time; _tweenDuration = Mathf.Max(0.1f, Mod.SavedSmoothingSec.value);
        }

        private System.Collections.IEnumerator ApplySunRotationAtEndOfFrame(Quaternion rot)
        {
            yield return new WaitForEndOfFrame();
            if (_sun != null) _sun.transform.rotation = rot;
        }

        #endregion

        #region UI

        private void BuildUI()
        {


            var view = UIView.GetAView();
            _panel = (UIPanel)view.AddUIComponent(typeof(UIPanel));
            _panel.backgroundSprite = "MenuPanel2";
            _panel.size = new Vector2(330f, 120f);
            _panel.relativePosition = new Vector3(Mod.SavedPanelX.value, Mod.SavedPanelY.value);
            _panel.canFocus = true;
            _panel.name = "AutoSunPanel";

            _panel.eventMouseDown += (c, p) => _panel.BringToFront();
            _panel.eventMouseMove += (c, p) =>
            {
                if ((p.buttons & UIMouseButton.Left) != 0)
                {
                    _panel.relativePosition = new Vector3(
                        _panel.relativePosition.x + p.moveDelta.x,
                        _panel.relativePosition.y - p.moveDelta.y,
                        _panel.relativePosition.z
                    );

                    Mod.SavedPanelX.value = _panel.relativePosition.x;
                    Mod.SavedPanelY.value = _panel.relativePosition.y;
                }
            };

            var title = _panel.AddUIComponent<UILabel>();
            title.relativePosition = new Vector3(8f, 6f);

            var lbl = _panel.AddUIComponent<UILabel>();
            lbl.relativePosition = new Vector3(8f, 28f);

            _valueLabel = _panel.AddUIComponent<UILabel>();
            _valueLabel.text = $"{_currentAzimuthDeg:0.0}°";
            _valueLabel.relativePosition = new Vector3(240f, 28f);

            _slider = _panel.AddUIComponent<UISlider>();
            _slider.width = 300f;
            _slider.height = 18f;
            _slider.relativePosition = new Vector3(8f, 50f);
            _slider.minValue = -180f;
            _slider.maxValue = 180f;
            _slider.stepSize = 0.5f;

            _slider.backgroundSprite = "ScrollbarTrack";



            var thumb = _slider.AddUIComponent<UISlicedSprite>();
            thumb.name = "Thumb";
            thumb.spriteName = "ScrollbarThumb";
            thumb.size = new Vector2(12f, 20f);
            thumb.relativePosition = new Vector3(0f, -6f);

            _slider.thumbObject = thumb;


            _slider.value = _currentAzimuthDeg;
            _slider.eventValueChanged += OnSliderChanged;

            title.text = I18n.T("panel.title");
            lbl.text = I18n.T("panel.long");
            _checkAuto = UIUtils.CreateCheckBox(_panel, new Vector2(8f, 74f),
                                                I18n.T("panel.autofollow"),
                                                Mod.SavedAutoFollow.value, v => { Mod.SavedAutoFollow.value = v; if (v) _lastCheck = 0f; });
            var manage = UIUtils.CreateCheckBox(_panel, new Vector2(_panel.width - 60f, 6f),
                I18n.T("panel.on"), Mod.SavedManageLongitude.value,
                isOn => {
                    Mod.SavedManageLongitude.value = isOn;
                    SetControlsEnabled(isOn);
                });

            _panel.isVisible = Mod.SavedPanelVisible.value;
        }

        private void OnSliderChanged(UIComponent c, float val)
        {
            Mod.SavedAutoFollow.value = false;
            if (_checkAuto != null) _checkAuto.isChecked = false;
            StartTweenTo(val);
            Mod.SavedLongitude.value = Normalize180(val);
        }

        private void SetControlsEnabled(bool enabled)
        {
            if (_slider != null) { _slider.isEnabled = enabled; _slider.isInteractive = enabled; _slider.opacity = enabled ? 1f : 0.4f; }
            if (_checkAuto != null) { _checkAuto.isEnabled = enabled; _checkAuto.isInteractive = enabled; _checkAuto.opacity = enabled ? 1f : 0.4f; }

        }

    }

    internal static class UIUtils
    {
        public static UICheckBox CreateCheckBox(UIComponent parent, Vector2 pos, string text, bool startValue, Action<bool> onChanged)
        {
            var cb = parent.AddUIComponent<UICheckBox>();
            cb.width = parent.width - 16f;
            cb.height = 20f;
            cb.relativePosition = new Vector3(pos.x, pos.y);

            var spriteUnchecked = cb.AddUIComponent<UISprite>();
            spriteUnchecked.spriteName = "check-unchecked";
            spriteUnchecked.size = new Vector2(16f, 16f);
            spriteUnchecked.relativePosition = new Vector3(0f, 2f);

            var spriteChecked = cb.AddUIComponent<UISprite>();
            spriteChecked.spriteName = "check-checked";
            spriteChecked.size = new Vector2(16f, 16f);
            spriteChecked.relativePosition = new Vector3(0f, 2f);
            cb.checkedBoxObject = spriteChecked;

            var label = cb.AddUIComponent<UILabel>();
            label.text = text;
            label.relativePosition = new Vector3(22f, 2f);

            cb.isChecked = startValue;
            cb.eventCheckChanged += (_, val) => { if (onChanged != null) onChanged(val); };

            return cb;
        }
    }

    #endregion

    // ThemeMixer2.5との共存
    internal sealed class ThemeMixerBridge
    {
        private Assembly _asm;
        private Type _tMgr;
        private object _mgr;
        private const BindingFlags F = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public ThemeMixerBridge()
        {
            try
            {
                var asms = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < asms.Length; i++)
                {
                    var a = asms[i];
                    if (a == null) continue;
                    var n = a.GetName().Name;
                    if (!string.IsNullOrEmpty(n) && n.IndexOf("ThemeMixer", StringComparison.OrdinalIgnoreCase) >= 0) { _asm = a; break; }
                }
                if (_asm != null)
                {
                    _tMgr = _asm.GetType("ThemeMixer.Themes.ThemeManager");
                    if (_tMgr != null)
                    {
                        var pI = _tMgr.GetProperty("Instance", F);
                        if (pI != null && pI.GetGetMethod(true) != null && pI.GetGetMethod(true).IsStatic) _mgr = pI.GetValue(null, null);
                        if (_mgr == null)
                        {
                            var fI = _tMgr.GetField("Instance", F);
                            if (fI != null && fI.IsStatic) _mgr = fI.GetValue(null);
                        }
                        if (_mgr == null && typeof(UnityEngine.Object).IsAssignableFrom(_tMgr))
                        {
                            var objs = Resources.FindObjectsOfTypeAll(_tMgr);
                            if (objs != null && objs.Length > 0) _mgr = objs[0];
                        }
                    }
                }
            }
            catch { }
        }

        private bool Ready => _asm != null && _tMgr != null && _mgr != null;
        private float _lastPushedLon;

        // ThemeMixerの経度に反映
        public void TryWriteLongitude(float longitudeDeg)
        {
            if (!Ready) return;
            if (Mathf.Abs(longitudeDeg - _lastPushedLon) < 0.01f) return;
            _lastPushedLon = longitudeDeg;

            try
            {
                object mix = null;
                var pMix = _tMgr.GetProperty("CurrentMix", F);
                if (pMix != null && pMix.CanRead) mix = pMix.GetValue(_mgr, null);
                if (mix == null)
                {
                    var fMix = _tMgr.GetField("CurrentMix", F);
                    if (fMix != null) mix = fMix.GetValue(_mgr);
                }
                if (mix == null) return;

                object atmo = null;
                var pAt = mix.GetType().GetProperty("Atmosphere", F);
                if (pAt != null && pAt.CanRead) atmo = pAt.GetValue(mix, null);
                if (atmo == null)
                {
                    var fAt = mix.GetType().GetField("Atmosphere", F);
                    if (fAt != null) atmo = fAt.GetValue(mix);
                }
                if (atmo == null) return;

                object lon = null;
                var pLo = atmo.GetType().GetProperty("Longitude", F);
                if (pLo != null && pLo.CanRead) lon = pLo.GetValue(atmo, null);
                if (lon == null)
                {
                    var fLo = atmo.GetType().GetField("Longitude", F);
                    if (fLo != null) lon = fLo.GetValue(atmo);
                }
                if (lon == null) return;

                var tLon = lon.GetType();
                var pUC = tLon.GetProperty("UseCustom", F);
                if (pUC != null && pUC.CanWrite && pUC.PropertyType == typeof(bool)) pUC.SetValue(lon, true, null);
                var fUC = tLon.GetField("m_UseCustom", F);
                if (fUC != null && fUC.FieldType == typeof(bool)) fUC.SetValue(lon, true);

                object cv = null;
                var pCV = tLon.GetProperty("CustomValue", F);
                if (pCV != null && pCV.CanRead) cv = pCV.GetValue(lon, null);
                if (cv == null)
                {
                    var fCV = tLon.GetField("CustomValue", F);
                    if (fCV != null) cv = fCV.GetValue(lon);
                }
                if (cv != null)
                {
                    var tCV = cv.GetType();
                    var fVal = tCV.GetField("m_value", F);
                    if (fVal != null && fVal.FieldType == typeof(float)) fVal.SetValue(cv, longitudeDeg);
                    else if (fVal != null && fVal.FieldType == typeof(double)) fVal.SetValue(cv, (double)longitudeDeg);
                }
            }
            catch { }
        }

        public bool TryReadLonLat(out float lonDeg, out float latDeg)
        {
            lonDeg = 0f; latDeg = 0f;
            if (!Ready) return false;

            try
            {
                object mix = null;
                var pMix = _tMgr.GetProperty("CurrentMix", F);
                if (pMix != null && pMix.CanRead) mix = pMix.GetValue(_mgr, null);
                if (mix == null)
                {
                    var fMix = _tMgr.GetField("CurrentMix", F);
                    if (fMix != null) mix = fMix.GetValue(_mgr);
                }
                if (mix == null) return false;

                object atmo = null;
                var pAt = mix.GetType().GetProperty("Atmosphere", F);
                if (pAt != null && pAt.CanRead) atmo = pAt.GetValue(mix, null);
                if (atmo == null)
                {
                    var fAt = mix.GetType().GetField("Atmosphere", F);
                    if (fAt != null) atmo = fAt.GetValue(mix);
                }
                if (atmo == null) return false;

                object lon = null, lat = null;
                var pLo = atmo.GetType().GetProperty("Longitude", F);
                if (pLo != null && pLo.CanRead) lon = pLo.GetValue(atmo, null);
                if (lon == null)
                {
                    var fLo = atmo.GetType().GetField("Longitude", F);
                    if (fLo != null) lon = fLo.GetValue(atmo);
                }

                var pLa = atmo.GetType().GetProperty("Latitude", F);
                if (pLa != null && pLa.CanRead) lat = pLa.GetValue(atmo, null);
                if (lat == null)
                {
                    var fLa = atmo.GetType().GetField("Latitude", F);
                    if (fLa != null) lat = fLa.GetValue(atmo);
                }

                if (lon == null || lat == null) return false;

                bool okL = TryReadAtmosphereFloat(lon, out lonDeg);
                bool okA = TryReadAtmosphereFloat(lat, out latDeg);
                return okL && okA;
            }
            catch { return false; }
        }

        private static bool TryReadAtmosphereFloat(object atmoFloat, out float val)
        {
            val = 0f;
            if (atmoFloat == null) return false;

            const BindingFlags FF = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            try
            {
                object cv = null;
                var pCV = atmoFloat.GetType().GetProperty("CustomValue", FF);
                if (pCV != null && pCV.CanRead) cv = pCV.GetValue(atmoFloat, null);
                if (cv == null)
                {
                    var fCV = atmoFloat.GetType().GetField("CustomValue", FF);
                    if (fCV != null) cv = fCV.GetValue(atmoFloat);
                }
                if (cv != null)
                {
                    var tCV = cv.GetType();
                    var f = tCV.GetField("m_value", FF);
                    if (f != null)
                    {
                        if (f.FieldType == typeof(float)) { val = (float)f.GetValue(cv); return true; }
                        if (f.FieldType == typeof(double)) { val = (float)(double)f.GetValue(cv); return true; }
                    }
                    var p = tCV.GetProperty("Value", FF);
                    if (p != null && p.CanRead)
                    {
                        var o = p.GetValue(cv, null);
                        if (o is float) { val = (float)o; return true; }
                        if (o is double) { val = (float)(double)o; return true; }
                    }
                }

                var pv = atmoFloat.GetType().GetProperty("Value", FF);
                if (pv != null && pv.CanRead)
                {
                    var o = pv.GetValue(atmoFloat, null);
                    if (o is float) { val = (float)o; return true; }
                    if (o is double) { val = (float)(double)o; return true; }
                }
            }
            catch { }
            return false;
        }
    }

    internal sealed class AutoSunEndFramePatcher : MonoBehaviour
    {
        private ThemeMixerBridge _tm;
        private Light _sun;

        public void SetThemeMixerBridge(ThemeMixerBridge tm) { _tm = tm; }

        private void Awake()
        {
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            FindSun();
        }

        private void LateUpdate()
        {
            if (!enabled) return;
            StartCoroutine(EndOfFrame());
        }

        private System.Collections.IEnumerator EndOfFrame()
        {
            yield return new WaitForEndOfFrame();
            ApplyFromThemeMixer();
        }

        private void FindSun()
        {
            var go = GameObject.Find("Sun") ?? GameObject.Find("Directional Light");
            if (go != null)
            {
                var li = go.GetComponent<Light>();
                if (li != null && li.type == LightType.Directional) { _sun = li; return; }
            }
            Light strongest = null; float i = -1f;
            var all = GameObject.FindObjectsOfType<Light>();
            for (int k = 0; k < all.Length; k++)
            {
                var li = all[k];
                if (li == null || li.type != LightType.Directional) continue;
                if (li.intensity > i) { i = li.intensity; strongest = li; }
            }
            _sun = strongest;
        }

        private void ApplyFromThemeMixer()
        {
            if (_tm == null) return;
            if (_sun == null) { FindSun(); if (_sun == null) return; }

            float lon, lat;
            if (!_tm.TryReadLonLat(out lon, out lat)) return;

            float az = lon * Mathf.Deg2Rad;
            float el = lat * Mathf.Deg2Rad;
            float cosE = Mathf.Cos(el);
            Vector3 dir = new Vector3(Mathf.Sin(az) * cosE, Mathf.Sin(el), Mathf.Cos(az) * cosE).normalized;

            _sun.transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);
        }
    }
}

internal static class IconAtlasLoader{
    // files[]: pngのフルパス。null可（無い場合は飛ばす）
    // names[]: スプライト名（filesと同じ長さ）
    public static UITextureAtlas CreateAtlasFromFiles(string atlasName, string[] files, string[] names)
    {
        try
        {
            var view = UIView.GetAView();
            var baseMat = UnityEngine.Object.Instantiate(view.defaultAtlas.material);
            baseMat.name = atlasName + "_Mat";

            // 有効な画像だけ読み込み
            var texList = new System.Collections.Generic.List<Texture2D>();
            var nameList = new System.Collections.Generic.List<string>();

            for (int i = 0; i < files.Length; i++)
            {
                var path = files[i];
                if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) continue;

                byte[] bytes = System.IO.File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                if (!tex.LoadImage(bytes)) continue;
                tex.name = (names != null && i < names.Length && !string.IsNullOrEmpty(names[i]))
                           ? names[i] : System.IO.Path.GetFileNameWithoutExtension(path);

                // 透過が荒い場合はフィルターをオフに
                tex.filterMode = FilterMode.Bilinear;

                texList.Add(tex);
                nameList.Add(tex.name);
            }

            if (texList.Count == 0) return null;

            // 1枚のテクスチャにパック
            var atlasTex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            var rects = atlasTex.PackTextures(texList.ToArray(), 2, 1024); // 余白2px, 最大1024

            var atlas = ScriptableObject.CreateInstance<UITextureAtlas>();
            atlas.name = atlasName;
            atlas.material = baseMat;
            atlas.material.mainTexture = atlasTex;

            for (int i = 0; i < texList.Count; i++)
            {
                var si = new UITextureAtlas.SpriteInfo
                {
                    name = nameList[i],
                    texture = atlasTex,
                    region = rects[i],          // PackTexturesが返したUV
                    border = new RectOffset()
                };
                atlas.AddSprite(si);
            }

            UnityEngine.Debug.Log($"[AutoSun] atlas '{atlasName}' created with {texList.Count} sprite(s).");
            return atlas;
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.Log($"[AutoSun] CreateAtlasFromFiles failed: {e.Message}");
            return null;
        }
    }
}
