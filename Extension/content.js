const WATCH_THRESHOLD_MS = 5 * 60 * 1000;

async function getInfo() 
{

  const url = window.location.href;
  const match = url.match(/miruro\.to\/watch\/(\d+)\/([^?]+)\?ep=(\d+)/);
  if (!match) return null;

  const anilistId = match[1];
  const episode = parseInt(match[3]);

  const stored = await browser.storage.local.get(`slugMap_${anilistId}`);
  const slug = stored[`slugMap_${anilistId}`] || match[2];

  console.log("Content script parsed:", { anilistId, slug, episode });
  return { anilistId, slug, episode };
}

(async () => {
  const info = await getInfo();
  if (!info) return;

  const startTime = Date.now();

  const interval = setInterval(async () => {
    const elapsed = Date.now() - startTime;
    
    if (elapsed >= WATCH_THRESHOLD_MS) 
    {
      clearInterval(interval);
      console.log("Watch threshold reached, sending WATCHED:", info);
      
      browser.runtime.sendMessage({
        type: "WATCHED",
        slug: info.slug,
        episodeNumber: info.episode,
        minutesWatched: Math.floor(elapsed / 60000)
      });
    }
  }, 30000);

  window.addEventListener("beforeunload", () => {
    clearInterval(interval);
  });
})();