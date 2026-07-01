// Provider interface — all source files must implement these properties
// name: string — display name
// icon: string — emoji or short label  
// canHandle: (show) => bool — whether this provider can build a URL for this show
// buildUrl: (anilistId, slug, episode) => string — constructs the watch URL