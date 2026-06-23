import eslint from "@eslint/js";
import tseslint from "typescript-eslint";
import importPlugin from "eslint-plugin-import-x";
import reactHooks from "eslint-plugin-react-hooks";

/**
 * Architectural boundaries (mirror of PFlow):
 *  - shared/ does not depend on features/ or app/
 *  - features/ do not depend on each other - composition lives only in app/
 */
export default tseslint.config(
  eslint.configs.recommended,
  ...tseslint.configs.recommended,
  {
    plugins: {
      "import-x": importPlugin,
      "react-hooks": reactHooks,
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      "import-x/no-restricted-paths": [
        "error",
        {
          zones: [
            {
              target: "./src/shared/**",
              from: "./src/features/**",
              message: "shared/ must not import from features/",
            },
            {
              target: "./src/shared/**",
              from: "./src/app/**",
              message: "shared/ must not import from app/",
            },
            {
              target: "./src/features/run/**",
              from: "./src/features/account/**",
              message: "features must not import each other; compose in app/",
            },
            {
              target: "./src/features/account/**",
              from: "./src/features/run/**",
              message: "features must not import each other; compose in app/",
            },
          ],
        },
      ],
      "@typescript-eslint/no-unused-vars": [
        "warn",
        { argsIgnorePattern: "^_", varsIgnorePattern: "^_" },
      ],
      "@typescript-eslint/no-explicit-any": "warn",
    },
  },
  {
    ignores: ["dist/**", "node_modules/**", "*.config.*", "vite.config.ts", "vitest.config.ts"],
  },
);
