import { defineConfig } from 'vitepress'

// https://vitepress.dev/reference/site-config
export default defineConfig({
  base: "/Sureyomichan/",
  title: "スレ詠みちゃん",
  description: "",
  head: [
    ["link", {rel: "icon", href: "/Sureyomichan/images/favicon.ico"}],
    ["meta", {property: "twitter:card", content: "summary"}],
    ["meta", {property: "twitter:site", content: "@HARUKei66494739"}],
    ["meta", {property: "twitter:description", content: "某所のスレッド読み上げツール"}],
    ["meta", {property: "twitter:image", content: "https://harukei66494739.github.io/Sureyomichan/og.png"}],
    ["meta", {property: "og:url", content: "https://harukei66494739.github.io/Sureyomichan/"}],
    ["meta", {property: "og:type", content: "product"}],
    ["meta", {property: "og:title", content: "スレ詠みちゃん"}],
    ["meta", {property: "og:description", content: "某所のスレッド読み上げツール"}],
    ["meta", {property: "og:image", content: "https://harukei66494739.github.io/Sureyomichan/og.png"}],
    ["meta", {property: "og:image:width", content: "64"}],
    ["meta", {property: "og:image:height", content: "64"}],
  ],
  themeConfig: {
    // https://vitepress.dev/reference/default-theme-config
    logo: "/images/logo-mini.svg",
    nav: [
      { text: "ホーム", link: "/" },
      { text: "使い方", link: "/usage/" }
    ],

    sidebar: {
      "/usage/": [
        {
          text: "使い方",
          items: [
            { text: "はじめに", link: "/usage/" },
            { text: "インストール・アンインストール手順", link: "/usage/install" },
            { text: "Chrome拡張", link: "/usage/chrome-extensions" },
            { text: "aimg API認証について", link: "/usage/aimg-auth" },
            { text: "設定画面説明", link: "/usage/settings" }, 
            { text: "使い方", link: "/usage/usage" }, 
            { text: "よくある質問", link: "/usage/faq" },
          ],
        }
      ]
    },

    socialLinks: [
      { icon: "github", link: "https://github.com/HARUKei66494739/Sureyomichan" }
    ]
  }
})
