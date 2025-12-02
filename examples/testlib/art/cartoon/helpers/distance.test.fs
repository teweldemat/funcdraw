{
  basic: {
    name: "computes Euclidean distance for point pairs";
    cases: [
      { input: [[0, 0], [3, 4]], expected: 5 },
      { input: [[-2, -2], [1, 2]], expected: 5 },
      { input: [null, [0, 0]], expected: 0 }
    ];
    test: (res, caseData) => assert.approx(res, caseData.expected, 0.000001)
  };

  normalizeHelper: {
    name: "uses ambient normalizePoint when provided";
    cases: [
      {
        ambient: null;
        input: [{ x: 2, y: 3 }, { x: 5, y: 7 }];
        expected: 5
      }
    ];
    test: (res, caseData) => assert.approx(res, caseData.expected, 0.000001)
  };

  return [basic, normalizeHelper];
}
