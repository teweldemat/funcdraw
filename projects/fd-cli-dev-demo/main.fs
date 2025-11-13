{
  aurora: import("aurora-library");
  cartoon: import("@funcdraw/cat-cartoon");

  meadow: aurora.meadow([-16, -10], [32, 22], {
    groundHeight: 9,
    sun: { offsetX: 0.2, heightRatio: 0.65, radius: 2.6 },
    hill: { backgroundColor: '#22c55e', foregroundColor: '#15803d' }
  });

  cottage: cartoon.city.house('cottage', [-10, -1.2], 8.5);
  rowhome: cartoon.city.house('townhome', [-1, -2], 7.2);
  modernHome: cartoon.city.house('modern', [10, -2], 12);
  coupe: cartoon.city.car([3.5, -3], 11);

  eval [
    meadow,
    cottage.graphics,
    rowhome.graphics,
    modernHome.graphics,
    coupe.graphics
  ];
}
