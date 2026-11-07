// Compatibility shim for the Visual Studio 2026 Vitest test adapter.
//
// The adapter that ships with Visual Studio imports `VitestTestRunner` from
// 'vitest/runners' during test discovery. Vitest 5 removed that entry point
// and exposes the runner as `TestRunner` from 'vitest'. vite.config.js aliases
// 'vitest/runners' to this file (in tests only), so the adapter keeps working.
//
// You can delete this file and the alias once Visual Studio's adapter supports Vitest 5.
export { TestRunner as VitestTestRunner } from 'vitest'
