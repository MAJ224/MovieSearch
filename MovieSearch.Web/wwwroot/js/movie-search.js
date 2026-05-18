const state = {
    pageIndex: 1,
    hasPreviousPage: false,
    hasNextPage: false,
    lastQuery: ""
};

const elements = {
    form: document.querySelector("#searchForm"),
    query: document.querySelector("#query"),
    provider: document.querySelector("#provider"),
    type: document.querySelector("#type"),
    year: document.querySelector("#year"),
    pageSize: document.querySelector("#pageSize"),
    searchButton: document.querySelector("#searchButton"),
    status: document.querySelector("#status"),
    results: document.querySelector("#results"),
    resultCount: document.querySelector("#resultCount"),
    pageInfo: document.querySelector("#pageInfo"),
    prevPage: document.querySelector("#prevPage"),
    nextPage: document.querySelector("#nextPage"),
    details: document.querySelector("#details")
};

elements.form.addEventListener("submit", event => {
    event.preventDefault();
    state.pageIndex = 1;
    searchMovies();
});

elements.prevPage.addEventListener("click", () => {
    if (!state.hasPreviousPage) {
        return;
    }

    state.pageIndex -= 1;
    searchMovies();
});

elements.nextPage.addEventListener("click", () => {
    if (!state.hasNextPage) {
        return;
    }

    state.pageIndex += 1;
    searchMovies();
});

loadProviders();

function setStatus(message, isError = false) {
    elements.status.textContent = message;
    elements.status.classList.toggle("error", isError);
}

async function loadProviders() {
    elements.provider.disabled = true;
    elements.searchButton.disabled = true;
    elements.provider.innerHTML = `<option value="">Loading</option>`;

    try {
        const response = await fetch("/api/movie/providers");
        const payload = await readPayload(response);

        if (!response.ok) {
            throw new Error(payload.message || "Could not load providers.");
        }

        renderProviders(payload.data);
    } catch (error) {
        renderProviders([]);
        setStatus(error.message || "Could not load providers.", true);
    } finally {
        elements.provider.disabled = false;
    }
}

async function searchMovies() {
    const query = elements.query.value.trim();
    const provider = elements.provider.value;

    if (!query) {
        setStatus("Query is required.", true);
        return;
    }

    if (!provider) {
        setStatus("Provider is required.", true);
        return;
    }

    state.lastQuery = query;
    setStatus("Searching...");
    elements.searchButton.disabled = true;
    elements.prevPage.disabled = true;
    elements.nextPage.disabled = true;

    try {
        const params = new URLSearchParams({
            query,
            provider,
            pageIndex: state.pageIndex.toString(),
            pageSize: elements.pageSize.value
        });

        if (elements.type.value) {
            params.set("type", elements.type.value);
        }

        if (elements.year.value) {
            params.set("year", elements.year.value);
        }

        const response = await fetch(`/api/movie/search?${params.toString()}`);
        const payload = await readPayload(response);
        const data = payload.data;

        if (!response.ok) {
            throw new Error(payload.message || "Search failed.");
        }

        renderResults(data);
        setStatus("");
    } catch (error) {
        renderResults(null);
        setStatus(error.message || "Search failed.", true);
    } finally {
        elements.searchButton.disabled = false;
        updatePager();
    }
}

async function loadDetails(id) {
    setStatus("Loading...");

    try {
        const params = new URLSearchParams({
            provider: elements.provider.value
        });

        const response = await fetch(`/api/movie/${encodeURIComponent(id)}?${params.toString()}`);
        const payload = await readPayload(response);

        if (!response.ok) {
            throw new Error(payload.message || "Movie not found.");
        }

        renderDetails(payload.data);
        setStatus("");
    } catch (error) {
        elements.details.innerHTML = `<div class="empty">${escapeHtml(error.message || "Movie not found.")}</div>`;
        setStatus(error.message || "Movie not found.", true);
    }
}

async function readPayload(response) {
    const text = await response.text();

    if (!text) {
        return { data: null, message: "" };
    }

    return JSON.parse(text);
}

function renderProviders(providers) {
    const availableProviders = Array.isArray(providers) ? providers : [];

    if (availableProviders.length === 0) {
        elements.provider.innerHTML = `<option value="">No providers</option>`;
        elements.searchButton.disabled = true;
        return;
    }

    elements.provider.innerHTML = availableProviders
        .map(provider => `<option value="${escapeHtml(provider)}">${escapeHtml(provider)}</option>`)
        .join("");

    elements.searchButton.disabled = false;
}

function renderResults(data) {
    const items = Array.isArray(data?.items) ? data.items : [];

    state.hasPreviousPage = Boolean(data?.hasPreviousPage);
    state.hasNextPage = Boolean(data?.hasNextPage);

    elements.resultCount.textContent = data
        ? `${data.totalCount} total`
        : "";

    elements.pageInfo.textContent = data
        ? `Page ${data.pageIndex} of ${Math.max(data.totalPages, 1)}`
        : "Page 1";

    if (items.length === 0) {
        elements.results.innerHTML = `<div class="empty">No results</div>`;
        return;
    }

    elements.results.innerHTML = items.map(movie => `
        <article class="movie-row">
            ${renderPoster(movie.poster, movie.title, "poster")}
            <div>
                <p class="movie-title">${escapeHtml(movie.title)}</p>
                <div class="meta">${escapeHtml(movie.year)} - ${escapeHtml(movie.type)}</div>
            </div>
            <button class="secondary" type="button" data-id="${escapeHtml(movie.imdbId)}">Details</button>
        </article>
    `).join("");

    elements.results.querySelectorAll("[data-id]").forEach(button => {
        button.addEventListener("click", () => loadDetails(button.dataset.id));
    });
}

function renderDetails(movie) {
    if (!movie) {
        elements.details.innerHTML = `<div class="empty">No details</div>`;
        return;
    }

    const ratings = Array.isArray(movie.ratings) && movie.ratings.length > 0
        ? movie.ratings.map(rating => `${escapeHtml(rating.source)}: ${escapeHtml(rating.value)}`).join("<br>")
        : "N/A";

    elements.details.innerHTML = `
        <div class="detail-head">
            ${renderPoster(movie.poster, movie.title, "detail-poster")}
            <div>
                <h3 class="detail-title">${escapeHtml(movie.title)}</h3>
                <div class="meta">${escapeHtml(movie.year)} - ${escapeHtml(movie.rated)} - ${escapeHtml(movie.runtime)}</div>
                <div class="meta">${escapeHtml(movie.genre)}</div>
            </div>
        </div>
        <p class="plot">${escapeHtml(movie.plot)}</p>
        <dl class="facts">
            ${renderFact("Director", movie.director)}
            ${renderFact("Actors", movie.actors)}
            ${renderFact("Country", movie.country)}
            ${renderFact("Awards", movie.awards)}
            ${renderFact("IMDb", `${movie.imdbRating || "N/A"} (${movie.imdbVotes || "N/A"})`)}
            ${renderFact("Ratings", ratings, true)}
        </dl>
    `;
}

function renderFact(label, value, allowHtml = false) {
    const content = allowHtml ? value : escapeHtml(value || "N/A");

    return `
        <div class="fact">
            <dt>${escapeHtml(label)}</dt>
            <dd>${content}</dd>
        </div>
    `;
}

function renderPoster(url, title, className) {
    if (!url || url === "N/A") {
        return `<div class="${className}">No poster</div>`;
    }

    return `
        <div class="${className}">
            <img src="${escapeHtml(url)}" alt="${escapeHtml(title)} poster" loading="lazy" />
        </div>
    `;
}

function updatePager() {
    elements.prevPage.disabled = !state.hasPreviousPage;
    elements.nextPage.disabled = !state.hasNextPage;
}

function escapeHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}
