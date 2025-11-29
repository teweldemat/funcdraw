const safeStatic = typeof staticMan === "function" ? staticMan : () => ({ graphics: [] });
const safeSteper = typeof steperMan === "function" ? steperMan : () => ({ graphics: [] });

return {
  static: safeStatic,
  steperMan: safeSteper
};
