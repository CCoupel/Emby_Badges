(function(){
  var CURRENT_MAJOR=parseInt(document.querySelector('meta[name="current-major"]').content,10);
  window.applyBadges=function(d){
    document.querySelectorAll('[data-badge-version]').forEach(function(el){
      var v=el.dataset.badgeVersion,m=parseInt(v.match(/^v(\d+)/)[1],10),diff=CURRENT_MAJOR-m;
      if(diff>=2){el.remove();return;}
      el.classList.toggle('badge-orange',diff===0);el.classList.toggle('badge-blue',diff===1);
      el.textContent=diff===0?((d&&d['badge.new'])||'Nouveau')+' '+v:v;
    });
  };
  window.applyBadges();
})();
