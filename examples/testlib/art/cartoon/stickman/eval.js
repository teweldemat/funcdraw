const safeStatic = typeof staticMan === "function" ? staticMan : () => ({ graphics: [] });
const safeSteperProfile = typeof steperManProfile === "function"
  ? steperManProfile
  : typeof steperMan === "function"
    ? steperMan
    : () => ({ graphics: [] });
const safeSteperZoom = typeof steperManZoom === "function" ? steperManZoom : () => ({ graphics: [] });
const safeSideWalkMan = typeof sideWalkMan === "function" ? sideWalkMan : () => ({ graphics: [] });
const safeZoomWalkMan = typeof zoomWalkMan === "function" ? zoomWalkMan : () => ({ graphics: [] });

return {
  static: safeStatic,
  steperManProfile: safeSteperProfile,
  steperManZoom: safeSteperZoom,
  sideWalkMan: safeSideWalkMan,
  zoomWalkMan: safeZoomWalkMan,
  // Legacy alias to ease the transition; remove when callers swap to steperManProfile
  steperMan: safeSteperProfile
};
