const tegakiStorage = {
    openTegakiContentForStart: true,
};
const iconPath = "M 171.636,417.455L 360.727,417.455L 360.727,493.091L 171.636,493.091L 171.636,417.455 Z M 171.636,190.545L 360.727,190.545L 360.727,266.182L 171.636,266.182L 171.636,190.545 Z M 133.818,304L 398.545,304L 398.545,379.636L 133.818,379.636L 133.818,304 Z M 171.636,530.909L 360.727,530.909L 360.727,606.545L 171.636,606.545L 171.636,530.909 Z M 572.477,190.546L 723.749,190.546L 723.749,266.182L 572.477,266.182L 572.477,190.546 Z M 398.545,757.818L 398.545,795.637L 398.545,833.455L 133.818,833.455L 133.818,795.637L 133.818,757.818L 133.818,720L 133.818,644.364L 398.545,644.364L 398.545,720L 398.545,757.818 Z M 302.992,720L 229.371,720L 229.371,757.818L 302.992,757.818L 302.992,720 Z M 867.523,723.542L 800.099,757.819L 724.931,609.958L 724.931,833.455L 649.295,833.455L 649.295,379.637L 512,379.636L 512,304L 723.749,304L 723.749,379.636L 724.931,379.637L 724.931,417.454L 890.182,417.454L 890.182,493.091L 750.369,493.091L 867.523,723.542 Z M 436.364,455.273L 610.295,455.273L 610.295,530.909L 610.295,757.818L 534.658,757.818L 534.658,530.909L 436.364,530.909L 436.364,455.273 Z ";

function getSureyomichanUrl() {
    const r = location.href.match(new RegExp("https://nijiurachan.net/(b|mix)/aimg/thread/([0-9a-f\\-]+)"));
    if(r) {
        return `sureyomichan://open/aimg/${r[2]}`;
    } else {
        return null;
    }
}


const targetNodeSpeechToolBox = "desktop-thread-toolbar-speech";
const targetNodeBottomButton = "desktop-thread-page";
const observerNode = "desktop-app";

// 現時点で未使用
function createToolBoxContainer() {
    const toolBox = document.body.querySelector(`.${targetNodeSpeechToolBox}`);
    if(!toolBox) {
        return null;
    }
    const defaultYomiage = toolBox.querySelector("span");

    const container = document.createElement("span");
    container.classList == [
        "__sureyomi_mode"
    ];
    const button = document.createElement("button");
    const span = document.createElement("span");
    const select = document.createElement("select");
    select.classList == [
        "__sureyomi_mode_select"
    ];
    const option1 = document.createElement("option");
    const option2 = document.createElement("option");
    const svg = document.body.querySelector(`.${targetNodeSpeechToolBox}`)
        .querySelector(".speech-mode-select")
        .querySelector("svg")
        .cloneNode(true);

    button.type = "button";
    button.ariaPressed = "false";
    button.textContent = "スレ詠みちゃん";
    option1.value = "__sureyomi_1"; // TODO:変数
    option1.textContent = "最初から";
    option2.value = "__sureyomi_2"; // TODO:変数
    option2.textContent = "途中から";

    span.appendChild(select);
    span.appendChild(svg);
    select.appendChild(option1);
    select.appendChild(option2);

    container.appendChild(button);
    container.appendChild(select);

    button.addEventListener("click", () => {
        console.log(select.value);
    });

    toolBox.appendChild(container);
    defaultYomiage.style.display = "none";
    return container;
}


(async() => {
    function createSureyomichanControl2(tegakiStorage, getSureyomichanUrl) {
        const container = document.createElement("div");
        container.classList.add("__sureyomi_start-button_root");

        const container2 = document.createElement("div");
        container2.classList.add("__sureyomi_start-button_continer");

        const button = document.createElement("button");
        button.classList.add("desktop-thread-reply-fab");
        button.classList.add("__sureyomi_start-button");

        const svg = document.createElementNS("http://www.w3.org/2000/svg","svg");
        const path = document.createElementNS("http://www.w3.org/2000/svg","path");
        svg.setAttribute("viewBox", "0 0 1024.00 1024.00");
        path.setAttribute("fill", "#ffffff");
        path.setAttribute("stroke-linejoin", "round");
        path.setAttribute("d", iconPath);
        svg.appendChild(path);

        const panel1 = document.createElement("div");
        const panel2 = document.createElement("div");

        const sureyomiBack = document.createElement("div");
        sureyomiBack.classList.add("__sureyomi_bk"); //# TODO: 名前
        sureyomiBack.style.background = "rgba(0, 0, 0, 0)";

        const sureyomi = m.createSureyomichanControl(tegakiStorage, getSureyomichanUrl);
        sureyomi.classList.add("__sureyomi_controll_component");

        panel1.append(button);
        button.appendChild(svg);
        panel1.append(sureyomi);
        container2.append(panel1);
        container2.append(panel2);
        container.append(container2);
        container.append(sureyomiBack);

        button.addEventListener("click", () => {
            sureyomi.classList.toggle("__sureyomi_open");
        });
        sureyomiBack.addEventListener("click", () => {
            sureyomi.classList.remove("__sureyomi_open");
        });

        return container;
    }


    const src = chrome.runtime.getURL("js/common.js");
    const m = await import(src);

    function updateButtonDataSet() {
        const desktop = document.body.querySelector(".desktop-app");
        const sureyomi = targetNode.querySelector(".__sureyomi_start-button_root");
        if(desktop) {
            if(desktop.dataset.replyButtonFlow) {
                sureyomi.dataset.layoutType = 2; // TODO: マジックナンバー
            } else {
                sureyomi.dataset.layoutType = 1;// TODO: マジックナンバー
            }
        } else {
            sureyomi.dataset.layoutType = 11;// TODO: マジックナンバー
        }
    }

    function tryCreateBottomButton() {
        const elm = document.body.querySelector(`.__sureyomi_start-button_root`);
        if(elm) {
            return elm;
        }

        const btn = document.body.querySelector(`#root`);
        const sureyomi = createSureyomichanControl2(tegakiStorage, getSureyomichanUrl);
        btn.appendChild(sureyomi);

        updateButtonDataSet();

        return sureyomi;
    }

    function init(appRoot, observer) {
        function act(appRoot) {
            const elm = appRoot.querySelector("div");
            if(!elm) {
                return;
            }

            updateButtonDataSet();

            if(!elm.classList.contains("desktop-app")) {
                return;
            }
            new MutationObserver((_) => {
                updateButtonDataSet();
            }).observe(
                elm,
                {
                    attributes: true,
                });
        }

        const root = (appRoot) ? appRoot : document.getElementById("app-root");
        if(!root) {
            return;
        }
        // app-rootが作られたので初期設定をする
        tryCreateBottomButton();

        act(root);
        new MutationObserver((_, __) => {
            act(root);
        }).observe(
            root,
            {
                attributes: true,
                childList: true,
            });
        observer?.disconnect();
    }
    const targetNode = document.getElementById("root");
    targetNode.classList.add("__sureyomi_hook");

    const appRoot = document.getElementById("app-root");
    if(appRoot) {
        init(appRoot, null);
    } else {
        new MutationObserver((_, observer) => {
            init(null, observer);
        }).observe(
            targetNode,
            {
                childList: true,
            });
    }
})();