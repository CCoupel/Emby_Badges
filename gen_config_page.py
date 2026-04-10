#!/usr/bin/env python3
"""Generates configPage.html (HTML fragment) and configScript.js (AMD module)."""
import base64, os

ICONS_DIR  = "src/EmbyBadges/Icons"
OUT_HTML   = "src/EmbyBadges/Configuration/configPage.html"
OUT_JS     = "src/EmbyBadges/Configuration/configScript.js"
PLUGIN_ID  = "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
CONTROLLER = "EmbyBadgesConfigScript"

def b64(name):
    with open(os.path.join(ICONS_DIR, name + ".png"), "rb") as f:
        return base64.b64encode(f.read()).decode()

res_icons  = {k: b64(k) for k in ("res_480p", "res_720p", "res_1080p", "res_4k")}
lang_icons = {k: b64(k) for k in ("lang_french", "lang_english", "lang_japanese")}

def img(key, data):
    return f'<img src="data:image/png;base64,{data}" class="eb-icon" alt="{key}">'

def group_settings(prefix):
    return f"""
          <div class="eb-grid">
            <div class="inputContainer">
              <label class="inputLabel inputLabelUnfocused">Position</label>
              <select id="{prefix}_Position" is="emby-select" class="emby-select-withcolor emby-input">
                <option value="TopLeft">Haut gauche</option>
                <option value="TopCenter">Haut centre</option>
                <option value="TopRight">Haut droite</option>
                <option value="CenterLeft">Centre gauche</option>
                <option value="CenterRight">Centre droite</option>
                <option value="BottomLeft">Bas gauche</option>
                <option value="BottomCenter">Bas centre</option>
                <option value="BottomRight">Bas droite</option>
              </select>
            </div>
            <div class="inputContainer">
              <label class="inputLabel inputLabelUnfocused">Taille (% image)</label>
              <input type="number" id="{prefix}_SizePercent" min="1" max="30" step="0.5" class="emby-input" />
            </div>
            <div class="inputContainer">
              <label class="inputLabel inputLabelUnfocused">Marge (% largeur)</label>
              <input type="number" id="{prefix}_MarginPercent" min="0" max="15" step="0.5" class="emby-input" />
            </div>
            <div class="inputContainer">
              <label class="inputLabel inputLabelUnfocused">Opacité (0–1)</label>
              <input type="number" id="{prefix}_Opacity" min="0" max="1" step="0.05" class="emby-input" />
            </div>
          </div>"""

RES_BADGES = (
    f'<label class="eb-toggle"><input type="checkbox" id="ShowSd" />'
    f'{img("res_480p", res_icons["res_480p"])}<span>SD</span></label>'
    f'<label class="eb-toggle"><input type="checkbox" id="ShowHd" />'
    f'{img("res_720p", res_icons["res_720p"])}<span>HD</span></label>'
    f'<label class="eb-toggle"><input type="checkbox" id="ShowFullHd" />'
    f'{img("res_1080p", res_icons["res_1080p"])}<span>Full HD</span></label>'
    f'<label class="eb-toggle"><input type="checkbox" id="Show4K" />'
    f'{img("res_4k", res_icons["res_4k"])}<span>4K</span></label>'
)

LANG_BADGES = (
    f'<label class="eb-toggle"><input type="checkbox" id="ShowFrench" />'
    f'{img("lang_french", lang_icons["lang_french"])}<span>Français</span></label>'
    f'<label class="eb-toggle"><input type="checkbox" id="ShowEnglish" />'
    f'{img("lang_english", lang_icons["lang_english"])}<span>Anglais</span></label>'
    f'<label class="eb-toggle"><input type="checkbox" id="ShowJapanese" />'
    f'{img("lang_japanese", lang_icons["lang_japanese"])}<span>Japonais</span></label>'
    f'<label class="eb-toggle"><input type="checkbox" id="ShowVo" />'
    f'<span class="eb-text-badge">VO</span><span>VO (langue originale non gérée, ex: coréen)</span></label>'
    f'<div style="width:100%;margin-top:8px">'
    f'<label class="eb-toggle"><input type="checkbox" id="HighlightOriginalLanguage" />'
    f'<span>Mettre en surbrillance la langue originale (contour doré sur le premier flux audio)</span></label>'
    f'</div>'
)

MULTI_BADGES = (
    '<label class="eb-toggle"><input type="checkbox" id="ShowMulti" />'
    '<span class="eb-text-badge eb-multi">MULTI</span><span>Activer le badge</span></label>'
    '<div class="inputContainer" style="margin-top:8px">'
    '<label class="inputLabel inputLabelUnfocused">Déclencheur</label>'
    '<select id="MultiVersionTrigger" is="emby-select" class="emby-select-withcolor emby-input">'
    '<option value="MultiVersionOnly">Uniquement si versions multiples</option>'
    '<option value="AlwaysForVirtualLib">Toujours si le média provient de VirtualLib</option>'
    '</select>'
    '<div class="fieldDescription">En mode VirtualLib, le badge affiche l\'initiale du connecteur source même pour les médias à version unique.</div>'
    '</div>'
)

FAV_BADGES = (
    '<label class="eb-toggle"><input type="checkbox" id="ShowFavorites" />'
    '<span class="eb-heart">&#10084;</span><span>Favori (au moins un utilisateur)</span></label>'
)

TMDB_SECTION = (
    '<div class="inputContainer">'
    '<label class="inputLabel inputLabelUnfocused">Clé API TMDB (optionnelle)</label>'
    '<input type="password" id="TmdbApiKey" class="emby-input" autocomplete="off" '
    'placeholder="ex\u00a0: 8265bd1679663a7ea12ac168da84d2e8" />'
    '<div class="fieldDescription">'
    'Sans clé\u00a0: langue originale estimée depuis les pays de production (moins fiable).<br>'
    'Avec clé\u00a0: langue originale exacte depuis TMDB.'
    '</div>'
    '<div style="margin-top:8px;display:flex;align-items:center;gap:10px">'
    '<button type="button" id="BtnTestTmdb" is="emby-button" class="emby-button">'
    '<span>Tester la clé</span>'
    '</button>'
    '<span id="TmdbTestStatus" style="font-size:0.9em"></span>'
    '</div>'
    '</div>'
    '<details style="margin-top:10px">'
    '<summary style="cursor:pointer;color:#52b54b;font-size:0.9em">Comment obtenir une clé API TMDB\u00a0?</summary>'
    '<ol style="margin:8px 0 0 20px;line-height:2">'
    '<li>Créer un compte sur <a href="https://www.themoviedb.org/signup" target="_blank" style="color:#52b54b">themoviedb.org</a></li>'
    '<li>Aller dans <b>Settings → API</b></li>'
    '<li>Cliquer <b>Create</b> → choisir <b>Developer</b></li>'
    '<li>Remplir le formulaire (usage personnel)</li>'
    '<li>Copier la <b>clé API (v3 auth)</b></li>'
    '</ol>'
    '</details>'
)

HTML = f"""<div is="emby-scroller" class="view flex flex-direction-column scrollFrameY flex-grow"
     data-horizontal="false" data-forcescrollbar="true" data-centerfocus="true"
     data-bindheader="true" data-controller="__plugin/{CONTROLLER}"
     data-title="Emby Badges">

  <div class="scrollSlider flex-grow flex-direction-column padded-left padded-left-page padded-right padded-top-page padded-bottom-page">
    <div id="EmbyBadgesConfigPage">

      <div class="sectionTitleContainer flex align-items-center">
        <h2 class="sectionTitle">Emby Badges</h2>
      </div>

      <style>
        .eb-icon {{ width:52px; height:34px; object-fit:contain; vertical-align:middle; }}
        .eb-text-badge {{ display:inline-block; padding:2px 10px; border-radius:4px;
          background:rgba(30,100,180,0.8); color:#fff; font-weight:bold; font-size:0.9em; }}
        .eb-multi {{ background:rgba(180,90,0,0.8); }}
        .eb-heart {{ display:inline-block; color:#dc1e3c; font-size:1.4em;
          line-height:1; vertical-align:middle; margin-right:2px; }}
        .eb-toggle {{ display:inline-flex; align-items:center; gap:6px;
          margin:0 16px 12px 0; cursor:pointer; }}
        .eb-toggles {{ margin-bottom:16px; padding-bottom:16px;
          border-bottom:1px solid rgba(255,255,255,0.1); }}
        .eb-grid {{ display:grid; grid-template-columns:1fr 1fr; gap:0 20px; }}
        .detailSection {{ margin-bottom:2em !important; padding-bottom:1em;
          border-bottom:2px solid rgba(255,255,255,0.12); }}
        .detailSection:last-of-type {{ border-bottom:none; }}
        .detailSectionHeader {{ font-size:1.05em; font-weight:600;
          padding-bottom:12px; margin-bottom:12px;
          border-bottom:1px solid rgba(255,255,255,0.15); }}
      </style>

      <!-- Global -->
      <div class="detailSection">
        <div class="checkboxContainer checkboxContainer-withDescription">
          <label>
            <input type="checkbox" id="EnableBadges" is="emby-checkbox" class="emby-checkbox" />
            <span>Activer les badges</span>
          </label>
        </div>
        <div class="checkboxContainer checkboxContainer-withDescription" style="margin-top:8px">
          <label>
            <input type="checkbox" id="DebugMode" is="emby-checkbox" class="emby-checkbox" />
            <span>Mode debug (affiche la langue originale et les flux audio sur chaque image)</span>
          </label>
        </div>
      </div>

      <!-- TMDB -->
      <div class="detailSection">
        <div class="detailSectionHeader">Intégration TMDB</div>
        {TMDB_SECTION}
      </div>

      <!-- Résolution -->
      <div class="detailSection">
        <div class="detailSectionHeader">Résolution</div>
        <div class="eb-toggles">{RES_BADGES}</div>
        {group_settings("Resolution")}
      </div>

      <!-- Langue -->
      <div class="detailSection">
        <div class="detailSectionHeader">Langue audio</div>
        <div class="eb-toggles">{LANG_BADGES}</div>
        {group_settings("Language")}
      </div>

      <!-- Multi-version -->
      <div class="detailSection">
        <div class="detailSectionHeader">Versions multiples</div>
        <div class="eb-toggles">{MULTI_BADGES}</div>
        {group_settings("MultiVersion")}
      </div>

      <!-- Favoris -->
      <div class="detailSection">
        <div class="detailSectionHeader">Favoris</div>
        <div class="eb-toggles">{FAV_BADGES}</div>
        {group_settings("Favorites")}
      </div>

      <div style="margin-top:1.5em">
        <button id="BtnSave" is="emby-button" class="raised emby-button">
          <span>Enregistrer</span>
        </button>
        <span id="SaveStatus" style="margin-left:1em;font-size:0.9em"></span>
      </div>

    </div>
  </div>
</div>
"""

JS = f"""/* global ApiClient, Dashboard */
define([], function () {{
    'use strict';

    var PLUGIN_ID = '{PLUGIN_ID}';

    return function (view) {{

        function setSelect(id, val) {{
            var el = view.querySelector('#' + id);
            if (!el) return;
            for (var i = 0; i < el.options.length; i++) {{
                if (el.options[i].value === val) {{ el.selectedIndex = i; break; }}
            }}
        }}

        function load() {{
            ApiClient.getPluginConfiguration(PLUGIN_ID).then(function (cfg) {{
                view.querySelector('#EnableBadges').checked = !!cfg.EnableBadges;
                view.querySelector('#DebugMode').checked   = !!cfg.DebugMode;
                view.querySelector('#TmdbApiKey').value = cfg.TmdbApiKey || '';

                view.querySelector('#ShowSd').checked      = !!cfg.ShowSd;
                view.querySelector('#ShowHd').checked      = !!cfg.ShowHd;
                view.querySelector('#ShowFullHd').checked  = !!cfg.ShowFullHd;
                view.querySelector('#Show4K').checked      = !!cfg.Show4K;
                view.querySelector('#ShowFrench').checked  = !!cfg.ShowFrench;
                view.querySelector('#ShowEnglish').checked = !!cfg.ShowEnglish;
                view.querySelector('#ShowJapanese').checked = !!cfg.ShowJapanese;
                view.querySelector('#ShowVo').checked      = !!cfg.ShowVo;
                view.querySelector('#HighlightOriginalLanguage').checked = cfg.HighlightOriginalLanguage !== false;
                view.querySelector('#ShowMulti').checked     = !!cfg.ShowMulti;
      setSelect('MultiVersionTrigger', cfg.MultiVersionTrigger || 'MultiVersionOnly');
      view.querySelector('#ShowFavorites').checked = !!cfg.ShowFavorites;

                ['Resolution', 'Language', 'MultiVersion', 'Favorites'].forEach(function (g) {{
                    var gc = cfg[g] || {{}};
                    setSelect(g + '_Position', gc.Position || 'BottomLeft');
                    view.querySelector('#' + g + '_SizePercent').value   = gc.SizePercent   != null ? gc.SizePercent   : 8;
                    view.querySelector('#' + g + '_MarginPercent').value = gc.MarginPercent != null ? gc.MarginPercent : 2;
                    view.querySelector('#' + g + '_Opacity').value       = gc.Opacity       != null ? gc.Opacity       : 0.92;
                }});
            }});
        }}

        function save() {{
            ApiClient.getPluginConfiguration(PLUGIN_ID).then(function (cfg) {{
                cfg.EnableBadges = view.querySelector('#EnableBadges').checked;
                cfg.DebugMode    = view.querySelector('#DebugMode').checked;
                cfg.TmdbApiKey   = view.querySelector('#TmdbApiKey').value.trim();

                cfg.ShowSd      = view.querySelector('#ShowSd').checked;
                cfg.ShowHd      = view.querySelector('#ShowHd').checked;
                cfg.ShowFullHd  = view.querySelector('#ShowFullHd').checked;
                cfg.Show4K      = view.querySelector('#Show4K').checked;
                cfg.ShowFrench    = view.querySelector('#ShowFrench').checked;
                cfg.ShowEnglish   = view.querySelector('#ShowEnglish').checked;
                cfg.ShowJapanese  = view.querySelector('#ShowJapanese').checked;
                cfg.ShowVo        = view.querySelector('#ShowVo').checked;
                cfg.HighlightOriginalLanguage = view.querySelector('#HighlightOriginalLanguage').checked;
                cfg.ShowMulti            = view.querySelector('#ShowMulti').checked;
      cfg.MultiVersionTrigger  = view.querySelector('#MultiVersionTrigger').value;
      cfg.ShowFavorites = view.querySelector('#ShowFavorites').checked;

                ['Resolution', 'Language', 'MultiVersion', 'Favorites'].forEach(function (g) {{
                    cfg[g] = {{
                        Position:      view.querySelector('#' + g + '_Position').value,
                        SizePercent:   parseFloat(view.querySelector('#' + g + '_SizePercent').value),
                        MarginPercent: parseFloat(view.querySelector('#' + g + '_MarginPercent').value),
                        Opacity:       parseFloat(view.querySelector('#' + g + '_Opacity').value)
                    }};
                }});

                ApiClient.updatePluginConfiguration(PLUGIN_ID, cfg).then(function () {{
                    Dashboard.processPluginConfigurationUpdateResult();
                }});
            }});
        }}

        function testTmdb() {{
            var key    = view.querySelector('#TmdbApiKey').value.trim();
            var status = view.querySelector('#TmdbTestStatus');
            if (!key) {{ status.textContent = 'Entrez une clé d\u2019abord.'; status.style.color = '#e0a000'; return; }}
            status.textContent = 'Test en cours\u2026';
            status.style.color = '';
            fetch('https://api.themoviedb.org/3/movie/550?api_key=' + encodeURIComponent(key))
                .then(function (r) {{
                    if (r.ok) {{
                        status.textContent = '\u2705 Clé valide\u00a0!';
                        status.style.color = '#52b54b';
                    }} else {{
                        status.textContent = '\u274c Clé invalide (HTTP\u00a0' + r.status + ')';
                        status.style.color = '#cc3333';
                    }}
                }})
                .catch(function () {{
                    status.textContent = '\u274c Erreur réseau';
                    status.style.color = '#cc3333';
                }});
        }}

        view.addEventListener('viewshow', load);
        view.querySelector('#BtnSave').addEventListener('click', save);
        view.querySelector('#BtnTestTmdb').addEventListener('click', testTmdb);
    }};
}});
"""

os.makedirs(os.path.dirname(OUT_HTML), exist_ok=True)
with open(OUT_HTML, "w", encoding="utf-8") as f:
    f.write(HTML)
with open(OUT_JS, "w", encoding="utf-8") as f:
    f.write(JS)
print(f"Generated {OUT_HTML} ({len(HTML):,} bytes)")
print(f"Generated {OUT_JS} ({len(JS):,} bytes)")
