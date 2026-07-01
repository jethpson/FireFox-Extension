const API_BASE = "https://user-service.ambitiousbush-0fcd2326.centralus.azurecontainerapps.io";
const API_URL = `${API_BASE}/api/schedule/catalog`;
const searchInput      = document.getElementById("search-input");
const resultsContainer = document.getElementById("results-container");

let trackedShowSlugs = [];

function createAddedBadge(slug) {
  return `
    <div class="tracked-badge" data-slug="${slug}" style="position:relative;display:inline-block;cursor:pointer;">
      <span class="badge-text" style="color:#27ae60;font-weight:bold;font-size:13px;padding:6px 12px;display:inline-block;">✓ Added</span>
      <span class="badge-remove" style="display:none;position:absolute;inset:0;background:rgba(220,38,38,0.9);color:white;font-weight:bold;font-size:13px;border-radius:4px;align-items:center;justify-content:center;">✕ Remove</span>
    </div>
  `;
}

function attachRemoveListener(container, slug, card) 
{

  const badge = container.querySelector(".tracked-badge");
  const removeOverlay = badge.querySelector(".badge-remove");

  badge.addEventListener("mouseenter", () => {
    removeOverlay.style.display = "flex";
  });

  badge.addEventListener("mouseleave", () => {
    removeOverlay.style.display = "none";
  });

  removeOverlay.addEventListener("click", async () => {
    removeOverlay.textContent = "Removing...";

    try 
    {

      const stored = await browser.storage.local.get("authToken");
      const token = stored.authToken;

      const res = await fetch(`${API_BASE}/api/user/tracked/${encodeURIComponent(slug)}`, {
        method: "DELETE",
        headers: { "Authorization": `Bearer ${token}` }
      });

      if (res.ok) 
      {

        trackedShowSlugs = trackedShowSlugs.filter(s => s !== slug);

        await browser.storage.local.remove("cachedShows");
        await browser.storage.local.remove("lastFetchedDate");

        const actionsContainer = card.querySelector(".show-actions");
        actionsContainer.innerHTML = `<button class="btn btn-primary add-btn" data-slug="${slug}" style="background-color:#af4e8a;color:white;border:none;padding:6px 12px;font-size:13px;border-radius:4px;cursor:pointer;">+ Add</button>`;
        const newBtn = actionsContainer.querySelector(".add-btn");

        newBtn.addEventListener("click", async () => {
          newBtn.disabled = true;
          newBtn.textContent = "Adding...";

          try 
          {

            const s2 = await browser.storage.local.get("authToken");

            const res2 = await fetch(`${API_BASE}/api/user/tracked`, {
              method: "POST",
              headers: { "Content-Type": "application/json", "Authorization": `Bearer ${s2.authToken}` },
              body: JSON.stringify({ slug })
            });

            if (res2.ok || res2.status === 409) 
            {

              trackedShowSlugs.push(slug);

              await browser.storage.local.remove("cachedShows");
              await browser.storage.local.remove("lastFetchedDate");

              actionsContainer.innerHTML = createAddedBadge(slug);

              attachRemoveListener(actionsContainer, slug, card);
            }

          } catch 
          {
             
            newBtn.textContent = "Error"; newBtn.disabled = false; 
          }
        });

      } else 
      {

        removeOverlay.textContent = "✕ Remove";
      }

    } catch 
    {

      removeOverlay.textContent = "✕ Remove";
    }

  });

}

function renderShows(shows) 
{

  resultsContainer.innerHTML = "";

  if (!shows.length) 
  {

    resultsContainer.innerHTML = `<p class="status" style="color:#6b6b6b;font-style:italic;text-align:center;">No shows found.</p>`;
    return;
  }

  shows.forEach(show => {

    const title  = show.title || "Unknown";
    const slug   = show.slug || "";
    const image  = show.imageUrl || "";
    const status = show.status || "";
    const isAlreadyTracked = trackedShowSlugs.includes(slug);

    const card = document.createElement("div");

    card.className = "show-card";
    card.setAttribute("style", "background:#f9f9fb;border:1px solid #dadadd;border-radius:8px;padding:14px 16px;margin-bottom:10px;display:flex;justify-content:space-between;align-items:center;gap:12px;");

    card.innerHTML = `
      <div style="display:flex;align-items:center;gap:12px;flex:1;">
        <img src="${image}" alt="${title}" style="width:48px;height:48px;border-radius:4px;object-fit:cover;flex-shrink:0;">
        <div class="show-info" style="font-family:Arial,sans-serif;color:#333;">
          <strong style="display:block;font-size:0.95rem;margin-bottom:2px;">${title}</strong>
          <small style="color:#6b6b6b;">${status}</small>
        </div>
      </div>
      <div class="show-actions"></div>
    `;

    resultsContainer.appendChild(card);

    const actionsContainer = card.querySelector(".show-actions");

    if (isAlreadyTracked) 
    {

      actionsContainer.innerHTML = createAddedBadge(slug);
      attachRemoveListener(actionsContainer, slug, card);
    } else 
    {

      actionsContainer.innerHTML = `<button class="btn btn-primary add-btn" data-slug="${slug}" style="background-color:#af4e8a;color:white;border:none;padding:6px 12px;font-size:13px;border-radius:4px;cursor:pointer;">+ Add</button>`;
      const btn = actionsContainer.querySelector(".add-btn");
      btn.addEventListener("click", async () => {
        btn.disabled = true;
        btn.textContent = "Adding...";

        try 
        {

          const stored = await browser.storage.local.get("authToken");
          const token = stored.authToken;
          const res = await fetch(`${API_BASE}/api/user/tracked`, {
            method: "POST",
            headers: { "Content-Type": "application/json", "Authorization": `Bearer ${token}` },
            body: JSON.stringify({ slug })
          });

          if (res.ok || res.status === 409) 
          {

            trackedShowSlugs.push(slug);
            actionsContainer.innerHTML = createAddedBadge(slug);
            attachRemoveListener(actionsContainer, slug, card);
          } else 
          {

            btn.textContent = "Failed";
            btn.disabled = false;
          }

        } catch (err) 
        {
          
          console.error("Add failed:", err);
          btn.textContent = "Error";
          btn.disabled = false;
        }

      });
    }

  });
}

searchInput.addEventListener("input", () => {
  loadShows(searchInput.value);
});

async function loadShows(query = "") 
{

  resultsContainer.innerHTML = `<p class="status" style="color:#6b6b6b;font-style:italic;text-align:center;">Loading…</p>`;

  try 
  {

    const stored = await browser.storage.local.get("authToken");
    const token = stored.authToken;

    if (!token) 
    {

      resultsContainer.innerHTML = `<p class="status" style="color:#6b6b6b;font-style:italic;text-align:center;">Please log in via the extension popup first.</p>`;
      return;
    }

    const url = query
      ? `${API_URL}?search=${encodeURIComponent(query)}`
      : API_URL;

    const [catalogRes, trackedRes] = await Promise.all([
      fetch(url, { method: "GET", headers: { "Content-Type": "application/json", "Authorization": `Bearer ${token}` } }),
      fetch(`${API_BASE}/api/user/tracked`, { method: "GET", headers: { "Content-Type": "application/json", "Authorization": `Bearer ${token}` } })
    ]);

    if (catalogRes.status === 401 || trackedRes.status === 401) 
    {

      resultsContainer.innerHTML = `<p class="status" style="color:#6b6b6b;font-style:italic;text-align:center;">Session expired. Please sign in again through the extension popup.</p>`;
      return;
    }

    if (!catalogRes.ok) throw new Error(`Catalog endpoint returned ${catalogRes.status}`);
    if (!trackedRes.ok) throw new Error(`Tracked endpoint returned ${trackedRes.status}`);

    const catalogData = await catalogRes.json();
    const trackedData = await trackedRes.json();

    const shows = Array.isArray(catalogData) ? catalogData : catalogData.shows ?? catalogData.results ?? [];
    const trackedList = Array.isArray(trackedData) ? trackedData : trackedData.shows ?? trackedData.results ?? [];

    trackedShowSlugs = trackedList.map(item => item.slug).filter(Boolean);

    renderShows(shows);

  } catch (err) 
  {
  
    console.error("loadShows failed:", err);
    resultsContainer.innerHTML = `<p class="status" style="color:#6b6b6b;font-style:italic;text-align:center;">Could not load shows. Check your API connection.</p>`;
  }
}

async function loadSidebar() 
{

  const sidebarEl = document.getElementById("sidebar-schedule");
  if (!sidebarEl) return;

  try 
  {

    const stored = await browser.storage.local.get("authToken");
    const token = stored.authToken;

    if (!token) { sidebarEl.innerHTML = `<p class="sidebar-loading">Sign in to see today's schedule.</p>`; return; }

    const res = await fetch(`${API_BASE}/api/schedule/today`, {
      headers: { "Authorization": `Bearer ${token}` }
    });

    if (!res.ok) throw new Error();
    const shows = await res.json();

    if (!shows.length) { sidebarEl.innerHTML = `<p class="sidebar-loading">No shows today.</p>`; return; }

    sidebarEl.innerHTML = shows.map(show => `
      <div class="sidebar-show">
        <img src="${show.imageUrl || ""}" alt="${show.title}">
        <div class="sidebar-show-info">
          <strong>${show.title}</strong>
          <small>Ep ${show.episodeNumber}</small>
        </div>
      </div>
    `).join("");

  } catch 
  {
  
    sidebarEl.innerHTML = `<p class="sidebar-loading">Could not load schedule.</p>`;
  }
}

document.addEventListener("DOMContentLoaded", () => {
  loadShows();
  loadSidebar();
});