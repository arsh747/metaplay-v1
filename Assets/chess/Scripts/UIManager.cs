using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChessGame
{

public class UIManager : MonoBehaviour
{
    PlayerInput playerInput;
    BoardUI boardUI;

    public PlayerUI playerWhite;
    public PlayerUI playerBlack;

    public GameObject capturePrefab;

    [SerializeField] CanvasGroup promotionMenu;

    [SerializeField] CanvasGroup winMenu;
    [SerializeField] TMP_Text winHeader;
    [SerializeField] TMP_Text winText;

    Coord promotionCoord;

    // True while the promotion choice menu is open and waiting for the
    // player's pick. PlayerInput checks this and ignores all board clicks
    // while it's true, so a click can never "fall through" to the board.
    public bool PromotionPending { get; private set; } = false;

    private void Start() {
        playerInput = GetComponent<PlayerInput>();
        boardUI = FindObjectOfType<BoardUI>();
    }

    public void FlipBoard(bool flipBoard){
        playerWhite.root.SetParent(flipBoard ? playerBlack.parent : playerWhite.parent);
        playerBlack.root.SetParent(flipBoard ? playerWhite.parent : playerBlack.parent);
        playerWhite.root.localPosition = Vector3.zero;
        playerBlack.root.localPosition = Vector3.zero;
    }

    public void AddCapturedPiece(int piece, bool isWhite){
        PlayerUI player = isWhite ? playerBlack : playerWhite;

        GameObject capture = Instantiate(capturePrefab);
        capture.transform.SetParent(player.capturesParent);

        Image image = capture.GetComponentInChildren<Image>();
        image.sprite = boardUI.theme.GetPieceSprite(piece);
    }
    
    public void OpenPromotionMenu(Coord coord){
        promotionCoord = coord;
        PromotionPending = true;

        // IMPORTANT: reactivate the menu GameObject every time. It gets
        // deactivated in SelectPromotionPiece() after a choice is made, so
        // without this line the menu would only ever work the very first
        // time - every promotion after that (for either color) would never
        // show up, leaving the game stuck waiting on a menu nobody can see.
        promotionMenu.gameObject.SetActive(true);

        StartCoroutine(FadeInMenu(promotionMenu));
    }

    public void SelectPromotionPiece(int piece){
        PromotionPending = false;

        // Reset the canvas group so it's ready clean for the *next* promotion
        promotionMenu.alpha = 0;
        promotionMenu.interactable = false;
        promotionMenu.blocksRaycasts = false;
        promotionMenu.gameObject.SetActive(false);

        if (promotionCoord != null){
            Coord coordToPromote = promotionCoord;
            promotionCoord = null;
            playerInput.PromoteAfterInput(coordToPromote, piece);
        }
    }

    public void OpenWinMenu(string header, string text){
        winHeader.text = header;
        winText.text = text;

        StartCoroutine(FadeInMenu(winMenu));
    }

    public void CloseWinMenu(){
        winMenu.alpha = 0;
        winMenu.interactable = false;
        winMenu.blocksRaycasts = false;

        foreach (Transform capture in playerWhite.capturesParent){
            Destroy(capture.gameObject);
        }

        foreach (Transform capture in playerBlack.capturesParent){
            Destroy(capture.gameObject);
        }
    }

    public void SetNames(string whiteName, string blackName){
        playerWhite.name.text = whiteName;
        playerBlack.name.text = blackName;
    }

    public void UpdateTimer(bool playerIsWhite, float time){
        int minutes = (int)time / 60;
        int seconds = (int)time % 60;

        string timeString = string.Format("{0:00}:{1:00}", minutes, seconds);

        if (playerIsWhite){
            playerWhite.timer.text = timeString;
        } else {
            playerBlack.timer.text = timeString;
        }
    }

    private IEnumerator FadeInMenu(CanvasGroup menu){
        menu.alpha = 0;

        // Block raycasts and allow interaction from frame 1, not just after
        // the fade finishes - otherwise a click during the fade would pass
        // straight through the menu onto the board underneath.
        menu.interactable = true;
        menu.blocksRaycasts = true;

        while (menu.alpha < 1){
            menu.alpha += Time.deltaTime;
            yield return null;
        }
    }
}

[System.Serializable]
public class PlayerUI{
    public Transform parent;
    public Transform root;
    public TMP_Text name;
    public TMP_Text timer;
    public Transform capturesParent;
}
}
