const safeStatic = typeof staticMan === "function" ? staticMan : () => ({ graphics: [] });
const safeSteperProfile = typeof steperManProfile === "function"
  ? steperManProfile
  : typeof steperMan === "function"
    ? steperMan
    : () => ({ graphics: [] });
const safeSteperZoom = typeof steperManZoom === "function" ? steperManZoom : () => ({ graphics: [] });

return {
  static: safeStatic,
  steperManProfile: safeSteperProfile,
  steperManZoom: safeSteperZoom,
  // Legacy alias to ease the transition; remove when callers swap to steperManProfile
  steperMan: safeSteperProfile
};
