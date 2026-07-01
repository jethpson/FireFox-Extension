export default 
{
    
  name: "Miruro",
  icon: "🎬",
  canHandle: (anilistId) => !!anilistId,
  buildUrl: (anilistId, slug, episode) =>
    `https://www.miruro.to/watch/${anilistId}/${slug}?ep=${episode}`
}