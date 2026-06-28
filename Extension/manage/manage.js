const API_URL = "https://user-service.ambitiousbush-0fcd2326.centralus.azurecontainerapps.io/api/schedule/catalog";

const searchInput      = document.getElementById("search-input");
const resultsContainer = document.getElementById("results-container");

function renderShows(shows) {
  resultsContainer.innerHTML = "";

  if (!shows.length) {
    resultsContainer.innerHTML = `<p class="status">No shows found.</p>`;
    return;
  }

  shows.forEach(show => {
    const title   = show.title || "Unknown";
    const slug    = show.slug || "";
    const image   = show.imageUrl || "";
    const status  = show.status || "";

    const card = document.createElement("div");
    card.className = "show-card";
    card.innerHTML = `
      <div style="display:flex;align-items:center;gap:12px;flex:1;">
        <img src="${image}" alt="${title}" style="width:48px;height:48px;border-radius:4px;object-fit:cover;flex-shrink:0;">
        <div class="show-info">
          <strong>${title}</strong>
          <small>${status}</small>
        </div>
      </div>
      <div class="show-actions">
        <button class="btn btn-primary">+ Add</button>
      </div>
    `;
    resultsContainer.appendChild(card);
  });
}

searchInput.addEventListener("input", () => {
  loadShows(searchInput.value);
});

async function loadShows(query = "") {
  resultsContainer.innerHTML = `<p class="status">Loading…</p>`;

  try {
    const stored = await browser.storage.local.get("authToken");
    const token = stored.authToken;

    if (!token) {
      resultsContainer.innerHTML = `<p class="status">Please log in via the extension popup first.</p>`;
      return;
    }

    const url = query
      ? `${API_URL}?search=${encodeURIComponent(query)}`
      : API_URL;

    const response = await fetch(url, {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
        "Authorization": `Bearer ${token}`
      }
    });

    if (response.status === 401) {
      resultsContainer.innerHTML = `<p class="status">Session expired. Please sign in again through the extension popup.</p>`;
      return;
    }

    if (!response.ok) throw new Error(`Server returned ${response.status}`);

    const data = await response.json();
    const shows = Array.isArray(data) ? data : data.shows ?? data.results ?? [];

    renderShows(shows);

  } catch (err) {
    console.error("loadShows failed:", err);
    resultsContainer.innerHTML = `<p class="status">Could not load shows. Check your API connection.</p>`;
  }
}

document.addEventListener("DOMContentLoaded", () => loadShows());