---
# https://vitepress.dev/reference/default-theme-home-page
layout: home

hero:
  name: "スレ詠みちゃん"
  text: "Sureyomichan"
  tagline: 某所のスレッドの読み上げを棒読みちゃんと連携して行います。
  image:
    src: /images/logo.png
    alt: logo
  actions:
      - theme: brand
        text: ダウンロード
        link: https://github.com/HARUKei66494739/Sureyomichan/releases
      - theme: alt
        text: 使い方
        link: /usage/

features:
  - title: ⚙基本機能
    details: 棒読みちゃんとの連携でスレッドの読み上げ。ファイルの自動取得。
  - title: 💻動作環境
    details: 動作には.NET Desktop Runtime v10の事前インストールが必要です。
  - title: 📚その他
    details: 今後良い感じに更新されるかもしれません。
---

<script setup lang="ts">
  // いい感じの方法ある気がするけどYAMLにv-bindする方法が見当たらないので強引に書き換える
  import { onMounted } from 'vue';
  onMounted(() => {
    const account = "HARUKei66494739";
    const repository = "Sureyomichan";
    fetch(`https://api.github.com/repos/${account}/${repository}/releases`)
      .then(function (res) {
        return res.json();
      }).then(function (json) {
        for(const release of json) {
          if(!release.draft && !release.prerelease) {
            for(const asset of release.assets) {
              console.log(asset.name);
              if(asset.name.match(/^sureyomichan-v.+\.zip$/)) {
                const a = document.querySelector(".actions .action a");
                if(a != null) {
                  a.innerText = `ダウンロード(${release.tag_name})`;
                  a.href = asset.browser_download_url;
                }
                return;
              }
            }
          }
        }
     });
  });
</script>