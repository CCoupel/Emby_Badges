/* global ApiClient, Dashboard */
define([], function () {
    'use strict';

    var PLUGIN_ID = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890';

    return function (view) {

        function setSelect(id, val) {
            var el = view.querySelector('#' + id);
            if (!el) return;
            for (var i = 0; i < el.options.length; i++) {
                if (el.options[i].value === val) { el.selectedIndex = i; break; }
            }
        }

        function load() {
            ApiClient.getPluginConfiguration(PLUGIN_ID).then(function (cfg) {
                view.querySelector('#EnableBadges').checked = !!cfg.EnableBadges;

                view.querySelector('#ShowSd').checked      = !!cfg.ShowSd;
                view.querySelector('#ShowHd').checked      = !!cfg.ShowHd;
                view.querySelector('#ShowFullHd').checked  = !!cfg.ShowFullHd;
                view.querySelector('#Show4K').checked      = !!cfg.Show4K;
                view.querySelector('#ShowFrench').checked  = !!cfg.ShowFrench;
                view.querySelector('#ShowEnglish').checked = !!cfg.ShowEnglish;
                view.querySelector('#ShowVo').checked      = !!cfg.ShowVo;
                view.querySelector('#ShowMulti').checked     = !!cfg.ShowMulti;
      setSelect('MultiVersionTrigger', cfg.MultiVersionTrigger || 'MultiVersionOnly');
      view.querySelector('#ShowFavorites').checked = !!cfg.ShowFavorites;

                ['Resolution', 'Language', 'MultiVersion', 'Favorites'].forEach(function (g) {
                    var gc = cfg[g] || {};
                    setSelect(g + '_Position', gc.Position || 'BottomLeft');
                    view.querySelector('#' + g + '_SizePercent').value   = gc.SizePercent   != null ? gc.SizePercent   : 8;
                    view.querySelector('#' + g + '_MarginPercent').value = gc.MarginPercent != null ? gc.MarginPercent : 2;
                    view.querySelector('#' + g + '_Opacity').value       = gc.Opacity       != null ? gc.Opacity       : 0.92;
                });
            });
        }

        function save() {
            ApiClient.getPluginConfiguration(PLUGIN_ID).then(function (cfg) {
                cfg.EnableBadges = view.querySelector('#EnableBadges').checked;

                cfg.ShowSd      = view.querySelector('#ShowSd').checked;
                cfg.ShowHd      = view.querySelector('#ShowHd').checked;
                cfg.ShowFullHd  = view.querySelector('#ShowFullHd').checked;
                cfg.Show4K      = view.querySelector('#Show4K').checked;
                cfg.ShowFrench  = view.querySelector('#ShowFrench').checked;
                cfg.ShowEnglish = view.querySelector('#ShowEnglish').checked;
                cfg.ShowVo      = view.querySelector('#ShowVo').checked;
                cfg.ShowMulti            = view.querySelector('#ShowMulti').checked;
      cfg.MultiVersionTrigger  = view.querySelector('#MultiVersionTrigger').value;
      cfg.ShowFavorites = view.querySelector('#ShowFavorites').checked;

                ['Resolution', 'Language', 'MultiVersion', 'Favorites'].forEach(function (g) {
                    cfg[g] = {
                        Position:      view.querySelector('#' + g + '_Position').value,
                        SizePercent:   parseFloat(view.querySelector('#' + g + '_SizePercent').value),
                        MarginPercent: parseFloat(view.querySelector('#' + g + '_MarginPercent').value),
                        Opacity:       parseFloat(view.querySelector('#' + g + '_Opacity').value)
                    };
                });

                ApiClient.updatePluginConfiguration(PLUGIN_ID, cfg).then(function () {
                    Dashboard.processPluginConfigurationUpdateResult();
                });
            });
        }

        view.addEventListener('viewshow', load);
        view.querySelector('#BtnSave').addEventListener('click', save);
    };
});
