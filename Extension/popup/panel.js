const API_URL = "https://user-service.ambitiousbush-0fcd2326.centralus.azurecontainerapps.io";

const showList   = document.getElementById("show-list");
const mainView   = document.getElementById("main-view");
const authView   = document.getElementById("auth-view");
const signInBtn  = document.getElementById("sign-in-btn");
const manageBtn  = document.getElementById("manage-btn");
const signOutBtn = document.getElementById("sign-out-btn");

async function getStoredToken() 
{
  try {
    const stored = await browser.storage.local.get("authToken");
    console.log("Storage result:", JSON.stringify(stored));
    return stored.authToken ?? null;
  } catch (err) {
    console.error("Storage error:", err);
    return null;
  }
}

async function storeToken(token) 
{

  await browser.storage.local.set({ authToken: token });
}

async function clearToken() 
{

  await browser.storage.local.remove("authToken");
}

function showAuthView() 
{

  authView.style.display  = "flex";
  mainView.style.display  = "none";
}

function showMainView() 
{

  authView.style.display  = "none";
  mainView.style.display  = "block";
}

function todayString() 
{

  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

function renderSchedule(shows) 
{
  showList.innerHTML = "";

  if (!shows || shows.length === 0) 
  {
    showList.innerHTML = `<li class="loading">No shows scheduled for today.</li>`;
    return;
  }

  shows.forEach(show => {
    const title   = show.title || "Unknown title";
    const episode = show.episodeNumber || "?";
    const image = show.imageUrl || "";
    const slug    = show.slug || "";

    const li = document.createElement("li");
    li.style.display = "flex";
    li.style.alignItems = "center";
    li.style.gap = "10px";
    li.innerHTML = `
      <img src="${image}" alt="${title}" style="width:48px;height:48px;border-radius:4px;object-fit:cover;flex-shrink:0;">
      <div>
        <strong>${title}</strong><br>
        <small>Episode ${episode}</small>
      </div>
    `;
    showList.appendChild(li);
  });
}

function renderError(message) 
{

  showList.innerHTML = `<li class="loading" style="color:#c0392b;">⚠ ${message}</li>`;
}

async function fetchSchedule(token) 
{

  const response = await fetch(`${API_URL}/api/schedule/my-today`, {
    method: "GET",
    headers: {
      "Content-Type": "application/json",
      "Authorization": `Bearer ${token}`
    }
  });

  if (response.status === 401) 
  {
    return [];
    //await clearToken();
    //showAuthView();
    //return;
  }

  if (!response.ok) throw new Error(`Server returned ${response.status}`);

  const data  = await response.json();
  const shows = Array.isArray(data) ? data : data.shows ?? data.results ?? [];

  await browser.storage.local.set({
    cachedShows: shows,
    lastFetchedDate: todayString()
  });

  return shows;
}

async function init() 
{
  await new Promise(resolve => setTimeout(resolve, 500));
  const token = await getStoredToken();
  console.log("Storage result:", JSON.stringify(await browser.storage.local.get()));

  if (!token) 
  {
    const token = await getStoredToken();
    console.log("init token:", token ? "found" : "not found");
    showAuthView();
    return;
  }

  showMainView();
  showList.innerHTML = `<li class="loading">Loading schedule…</li>`;

  try 
  {

    const stored = await browser.storage.local.get(["cachedShows", "lastFetchedDate"]);
    const today  = todayString();

    if (stored.lastFetchedDate === today && stored.cachedShows?.length) 
    {

      renderSchedule(stored.cachedShows);
      return;
    }

    const shows = await fetchSchedule(token);
    if (shows) renderSchedule(shows);

  } catch (err) 
  {

    console.error("init failed:", err);
    const stored = await browser.storage.local.get("cachedShows");
    if (stored.cachedShows) 
    {

      renderError("Couldn't refresh — showing cached data.");
      renderSchedule(stored.cachedShows);
    } else 
    {

      renderError("Couldn't reach the server.");
    }
  }
}

signInBtn.addEventListener("click", () => {
  browser.runtime.sendMessage({ type: "START_AUTH" });
});

signOutBtn.addEventListener("click", async () => {
  await clearToken();
  showAuthView();
});

manageBtn.addEventListener("click", () => {
  browser.tabs.create({ url: browser.runtime.getURL("manage/manage.html") });
});

browser.runtime.onMessage.addListener((msg) => {
  if (msg.type === "DAILY_FETCH_DONE") 
  {

    browser.storage.local.get("cachedShows").then(s => renderSchedule(s.cachedShows));
  }
  if (msg.type === "AUTH_SUCCESS") 
  {
    
    storeToken(msg.token).then(init);
  }
});

document.addEventListener("DOMContentLoaded", init);