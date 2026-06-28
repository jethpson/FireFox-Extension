const CLIENT_ID = "ea1f8538-33ce-49ba-babb-09ccd11b1e5e";
const TENANT_ID = "102d9167-6ab2-43be-81a9-353e194b8ee9";
const API_URL = "https://user-service.ambitiousbush-0fcd2326.centralus.azurecontainerapps.io";
const SCOPE = `openid profile email`;

function todayString() 
{

  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

async function signIn() 
{
  console.log("signIn started");
  const redirectURL = browser.identity.getRedirectURL();
  console.log("redirectURL:", redirectURL);

const authURL = `https://login.microsoftonline.com/common/oauth2/v2.0/authorize` +
    `?client_id=${CLIENT_ID}` +
    `&response_type=token` +
    `&redirect_uri=${encodeURIComponent(redirectURL)}` +
    `&scope=${encodeURIComponent(SCOPE)}` +
    `&response_mode=fragment`;

  try 
  {

    const responseURL = await browser.identity.launchWebAuthFlow({
      url: authURL,
      interactive: true
    });

    console.log("responseURL:", responseURL);

    const params = new URLSearchParams(new URL(responseURL).hash.slice(1));
    const token = params.get("access_token");

    if (!token) throw new Error("No token in response");

    await browser.storage.local.set({ authToken: token });

    const verify = await browser.storage.local.get("authToken");
    console.log("Token stored:", verify.authToken ? "yes" : "no");

    await fetch(`${API_URL}/api/auth/login`, {
      method: "POST",
      headers: { "Authorization": `Bearer ${token}` }
    });

    browser.runtime.sendMessage({ type: "AUTH_SUCCESS", token });

  } catch (err) 
  {

    console.error("Sign in failed:", err);
  }
}

async function fetchAndUpdateBadge() 
{

  try 
  {

    const stored = await browser.storage.local.get("authToken");
    const token = stored.authToken;

    if (!token) return;

    const response = await fetch(`${API_URL}/api/schedule/my-today`, {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
        "Authorization": `Bearer ${token}`
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

browser.runtime.onMessage.addListener(async (msg) => {
  if (msg.type === "START_AUTH") 
  {
    
    await signIn();
  }
});

browser.runtime.onInstalled.addListener(() => {
  console.log("Media Scheduler initialized.");
  initOnStartup();
});

browser.runtime.onStartup.addListener(() => {
  initOnStartup();
});