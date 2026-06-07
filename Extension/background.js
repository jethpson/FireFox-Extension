const API_URL = "https://YOUR_GATEWAY.azure-api.net/shows/today";

function todayString() 
{

  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

async function fetchAndUpdateBadge() 
{

  try 
  {

    const response = await fetch(API_URL, {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
        // "Ocp-Apim-Subscription-Key": "YOUR_KEY"
      }
    });

    if (!response.ok) throw new Error(`Server returned ${response.status}`);

    const data  = await response.json();
    const shows = Array.isArray(data) ? data : data.shows ?? data.results ?? [];

    await browser.storage.local.set({
      cachedShows: shows,
      lastFetchedDate: todayString()
    });

    const count = shows.length;
    browser.browserAction.setBadgeText({ text: count > 0 ? String(count) : "" });
    browser.browserAction.setBadgeBackgroundColor({ color: "#e74c3c" });

    browser.runtime.sendMessage({ type: "DAILY_FETCH_DONE" }).catch(() => {});

  } catch (err) 
  {

    console.error("fetchAndUpdateBadge failed:", err);
    browser.browserAction.setBadgeText({ text: "!" });
    browser.browserAction.setBadgeBackgroundColor({ color: "#e67e22" });
  }
}

async function initOnStartup() 
{

  const stored = await browser.storage.local.get(["cachedShows", "lastFetchedDate"]);
  const today  = todayString();

  if (stored.lastFetchedDate === today && stored.cachedShows?.length) 
  {

    const count = stored.cachedShows.length;
    browser.browserAction.setBadgeText({ text: String(count) });
    browser.browserAction.setBadgeBackgroundColor({ color: "#e74c3c" });
  } else 
  {
    
    await fetchAndUpdateBadge();
  }
}

browser.runtime.onInstalled.addListener(() => {
  console.log("Media Scheduler initialized.");
  initOnStartup();
});

browser.runtime.onStartup.addListener(() => {
  initOnStartup();
});