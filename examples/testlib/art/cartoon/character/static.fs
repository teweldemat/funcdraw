(anchor, measurements, palette, skin) =>
{
  geometry: skeleton.build(anchor, measurements);
  selectedSkin: skin??skins.stick;

  eval selectedSkin(geometry, palette);
}
