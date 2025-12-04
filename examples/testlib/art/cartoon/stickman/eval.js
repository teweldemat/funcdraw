function pick(name) {
  if (typeof name === 'string') {
    const direct = typeof globalThis !== 'undefined' ? globalThis[name] : undefined;
    if (direct !== undefined) return direct;
  }
  return undefined;
}

const staticManExport =
  typeof staticMan !== 'undefined' ? staticMan : typeof staticman !== 'undefined' ? staticman : pick('staticman');
const steperProfileExport =
  typeof steperManProfile !== 'undefined'
    ? steperManProfile
    : typeof stepermanprofile !== 'undefined'
      ? stepermanprofile
      : pick('stepermanprofile');
const steperZoomExport =
  typeof steperManZoom !== 'undefined'
    ? steperManZoom
    : typeof stepermanzoom !== 'undefined'
      ? stepermanzoom
      : pick('stepermanzoom');
const sideWalkExport =
  typeof sideWalkMan !== 'undefined' ? sideWalkMan : typeof sidewalkman !== 'undefined' ? sidewalkman : pick('sidewalkman');
const zoomWalkExport =
  typeof zoomWalkMan !== 'undefined' ? zoomWalkMan : typeof zoomwalkman !== 'undefined' ? zoomwalkman : pick('zoomwalkman');

return {
  static: staticManExport,
  steperManProfile: steperProfileExport,
  steperManZoom: steperZoomExport,
  sideWalkMan: sideWalkExport,
  zoomWalkMan: zoomWalkExport
};
