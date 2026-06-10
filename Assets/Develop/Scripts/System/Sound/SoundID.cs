using UnityEngine;

public enum SoundID
{
    // ===================================== BGM =====================================

    // **** タイトル ****
    BGM_TITLE,

    // **** セレクト画面 ****
    BGM_SELECT,

    // **** インゲーム ****
    BGM_RANK_1,
    BGM_RANK_2,
    BGM_RANK_3,

    // **** リザルト画面 ****
    BGM_RESULT,


    // ===================================== SE =====================================

    // **** タイトル ****
    SE_TITLE_START_BUTTON,  // スタートボタンを押したときの音
    SE_TITLE_CURSOR_MOVE,   // カーソル移動音
    SE_TITLE_END_BUTTON,    // 終了ボタンを押したときの音

    // **** セレクト画面 ****
    SE_SELECT_CURSOR_MOVE_BOARD,  // カーソル移動音(ボード)
    SE_SELECT_CURSOR_MOVE_STAGE,  // カーソル移動音(ステージ)
    SE_SELECT_DECIDE_BOARD,       // ボード決定音
    SE_SELECT_DECIDE_STAGE,       // ステージ決定音

    // **** インゲーム ****
    SE_INGAME_TIMER_TICK,           // タイマー1秒カウント音
    SE_INGAME_TIMEER_WARNING,       // タイマー残り30秒警告音

    SE_INGAME_PLAYER_MOVE,          // プレイヤー移動音
    SE_INGAME_PLAYER_MISS_PRESSED,  // プレイヤーミス押下音
    SE_INGAME_PLAYER_MISS_RELEASE,  // プレイヤーミス解放音
    SE_INGAME_PLAYER_GET_KEY,       // プレイヤー鍵取得音
    SE_INGAME_PLAYER_UNLOCK,       // プレイヤー解除
    SE_INGAME_PLAYER_NOT_VISIT,       // プレイヤー行くことができないタイルの時の音
    SE_INGAME_PLAYER_VISIT_SAFETILE,       // プレイヤー行くことができないタイルの時の音

    SE_INGAME_GIMMICK_WALL_ATTACK,     // ギミック壁当たり音
    SE_INGAME_GIMMICK_ENEMY_HIT,        // ギミック敵ヒット音

    SE_INGAME_STAGE_CLEARED,        // ステージクリア音
    SE_INGAME_STAGE_FAILED,         // ステージ失敗音

    SE_INGAME_MASHING


    // **** UI ****
    //SE_UI_BUTTON_BACK, // 戻るボタン
    //SE_UI_BUTTON_PUSH, // 決定ボタン
    //SE_UI_BUTTON_MOVE, // カーソル移動
    //SE_UI_BUTTON_GAMESTART_PUSH, // ゲームスタートボタンを押したときの音


    // **** ポーズ画面 ****
    //SE_PAUSE_OPEN,      // ポーズ画面オープン音
    //SE_PAUSE_CLOSE,     // ポーズ画面クローズ音


    //SE_INGAME_CLEAR,               // ゴール音
    //SE_RESULT_TIME_COUNTING,    // リザルトタイムカウント音

}