const API_URL = "https://user-service.ambitiousbush-0fcd2326.centralus.azurecontainerapps.io/api/schedule/today";

const searchInput      = document.getElementById("search-input");
const resultsContainer = document.getElementById("results-container");

let allShows = [];

function renderShows(shows) {
  resultsContainer.innerHTML = "";

  if (!shows.length) {
    resultsContainer.innerHTML = `<p class="status">No shows found.</p>`;
    return;
  }

  shows.forEach(show => {
    const name     = show.name     || show.title    || "Unknown";
    const time     = show.airTime  || show.time      || "TBD";
    const platform = show.platform || show.service   || "";
    const episode  = show.episode  || show.episodeTitle || null;

    const card = document.createElement("div");
    card.className = "show-card";
    card.innerHTML = `
      <div class="show-info">
        <strong>${name}</strong>
        <small>${time}${platform ? " · " + platform : ""}</small>
        ${episode ? `<br><small style="opacity:0.7">Ep: ${episode}</small>` : ""}
      </div>
      <div class="show-actions">
        <button class="btn btn-ghost">Details</button>
        <button class="btn btn-primary">+ Add</button>
      </div>
    `;
    resultsContainer.appendChild(card);
  });
}

function filterShows(query) {
  if (!query.trim()) return allShows;
  const q = query.toLowerCase();
  return allShows.filter(s =>
    (s.name || s.title || "").toLowerCase().includes(q) ||
    (s.platform || s.service || "").toLowerCase().includes(q)
  );
}

searchInput.addEventListener("input", () => {
  renderShows(filterShows(searchInput.value));
});

async function loadShows() {
  resultsContainer.innerHTML = `<p class="status">Loading…</p>`;

  try {
    // Retrieve the auth token saved by the background/popup flow
    const stored = await browser.storage.local.get("authToken");
    const token = stored.authToken;

    if (!token) {
      resultsContainer.innerHTML = `<p class="status">Please log in via the extension popup first.</p>`;
      return;
    }

    const response = await fetch(API_URL, {
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
    allShows   = Array.isArray(data) ? data : data.shows ?? data.results ?? [];

    renderShows(allShows);

  } catch (err) {
    console.error("loadShows failed:", err);
    resultsContainer.innerHTML = `<p class="status">Could not load shows. Check your API connection.</p>`;
  }
}

// Initialize on DOM load
document.addEventListener("DOMContentLoaded", loadShows);