(anchor, measurements, palette, skin) =>
{
  geometry: skeleton.build(anchor, measurements);
  selectedSkin: if skin == null then skins.stick else skin;

  eval selectedSkin(geometry, palette);
}
