(function(){
  var cache={};
  function apply(d){document.querySelectorAll('[data-i18n]').forEach(function(el){var k=el.dataset.i18n;if(d[k]!==undefined)el.textContent=d[k];});}
  function load(l){
    if(window.__LOCALES&&window.__LOCALES[l]){return Promise.resolve(window.__LOCALES[l]);}
    if(cache[l])return Promise.resolve(cache[l]);
    return fetch('locales/'+l+'.json').then(function(r){return r.json();}).then(function(d){cache[l]=d;return d;});
  }
  function set(l){
    load(l).then(function(d){
      apply(d);document.documentElement.lang=l;
      document.querySelectorAll('.lang-btn').forEach(function(b){b.classList.toggle('active',b.dataset.lang===l);});
      if(window.applyBadges)window.applyBadges(d);
      try{localStorage.setItem('lang',l);}catch(e){}
    });
  }
  document.querySelectorAll('.lang-btn').forEach(function(b){b.addEventListener('click',function(){set(b.dataset.lang);});});
  var l='fr';try{l=localStorage.getItem('lang')||((navigator.language||'fr').slice(0,2)==='en'?'en':'fr');}catch(e){}
  set(l);
})();
