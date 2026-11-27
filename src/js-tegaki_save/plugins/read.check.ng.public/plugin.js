// @ts-check
// read.check.ng.public
// 読み上げ時のプラグイン[NG機能 共通辞書]
// Ver2.1.0

import {
  MATCH_NONE,
  MATCH_REGEX,
  MATCH_LAMBDA,
  MATCH_CONDITIONS,
  MATCH_AUTO,
  MATCH_EX_IGNORE_CASE,
  MATCH_EX_LINEFEED,
  MATCH_EX_REMOVE_HTML_TAG,
  MATCH_EX_DECODE_SPECIAL_CHARCTER,
  MATCH_REGEX_INDICES,
  MATCH_REGEX_GLOBAL,
  MATCH_REGEX_IGNORE_CASE,
  MATCH_REGEX_MULTILINE,
  MATCH_REGEX_DOT_ALL,
  MATCH_REGEX_UNICODE,
  MATCH_REGEX_UNICODE_SETS,
  MATCH_REGEX_STICY,

  option,
  result,
  run
} from "../_common.ng/ng.js";

// プラグイン用実行関数（コメント読み上げの前処理）
export const pluginList = [
  {
    point: "read",
    sort: "010",
    beforeExecute: function (param) {
      return checkNg(param);;
    }
  }
];


/**
 * NG機能 共通辞書処理
 * @param {PluginParam} param 
 * @returns {PluginResult}
 */
function checkNg(param) {
  // メイン処理
  /** @type {import("../_common.ng/ng.js").NgTemplate[]} */
  const list = new Array();
  // 共通NGワードの配列
  list[list.length] = {matchType : option(MATCH_REGEX, [MATCH_EX_LINEFEED]) , value : "\&lt;(.*?)\&gt;"};  // 本文内にタグを含む場合
  list[list.length] = {matchType : option(MATCH_REGEX) , value : "((匿名(掲示板|コミュニティ)|自己(主張|顕示欲満々)|雑魚配信者|小遣い稼ぎ|スタートアップ|宣伝|乞食|コジキ|(お外|おそと|他所|よそ|ヨソ)で(やって|やれ|お願い)|にお帰りください|お客(さま|様)|巣に帰れ|ゴミ配信)[\s\S]*){2}|コテと(信者|取り巻き)|(キモい|気持ち悪い|くっさい)馴れ合い|ぶいちゅーばー|ついった～|「」のフリしろ|馴れ合う場所"};           // NGにしたいワードを""内に入れる(正規表現可能)
  list[list.length] = {matchType : option(MATCH_LAMBDA), value : (
    template,
    comment,
    commentFormat,
    res,
    ret) => {
    // 参照元がNG対象だった場合連鎖NG

    const format = (com) => {
      return com.replace(/<br>|\r\n|\r/g, "\n").replace(/<[^>]*>/g, "").replace(/&([^;]+);/g,
        (_, p1) => {
        // 今回は>だけの復元でよい
        switch(p1.toLowerCase()) {
          case "gt": return ">";
        }
        return p1;
      });
    };


    const test = () => {
      // NG判定処理
      // NGだとreturn true

      if(!res.__tegaki_res) {
        return false;
      }

      for(const lin of format(res.com).split("\n")) {
        if(/^>+/.test(lin)) {
          const srh = lin.replace(/^>+/, "");
          for(const it of res.__tegaki_res) {
            if(it.resNo === res.resNo) {
              break;
            }
            for(const target of format(it.com).split("\n")) {
              if(target.startsWith(srh)) {
                if(it.__tageki_ng || it.id.length || it.del.length) {
                  return true;
                } else {
                  break;
                }
              }
            }
          }
        }        
      }
      return false;
    };
    
    return result(test(), ret);
  }};  
  list[list.length] = {matchType : option(MATCH_LAMBDA), value : (
    template,
    comment,
    commentFormat,
    res,
    ret) => {

    if(res.__sureyomi_board_id || res.__sureyomi_board_id != "FutabaImg") {
      return result(false, ret);
    }
    if(param.res.fsize === 0) {
      return result(false, ret);
    }
    if(param.res.ext.toLowerCase() === ".png") {
      return result(false, ret);
    }

    result(true, ret)
  }};

  list[list.length] = {matchType : option(MATCH_NONE) , value : ""};
  list[list.length] = {matchType : option(MATCH_NONE) , value : ""};
  list[list.length] = {matchType : option(MATCH_NONE) , value : ""};
  list[list.length] = {matchType : option(MATCH_NONE) , value : ""};
  list[list.length] = {matchType : option(MATCH_NONE) , value : ""};
  list[list.length] = {matchType : option(MATCH_NONE) , value : ""};
  list[list.length] = {matchType : option(MATCH_NONE) , value : ""};

  return run(param, list);
}




