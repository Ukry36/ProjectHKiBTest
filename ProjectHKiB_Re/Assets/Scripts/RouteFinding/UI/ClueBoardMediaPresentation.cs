using UnityEngine;

namespace RouteFinding.UI
{
    // 단서 보드의 표현 형식. 데이터의 분류(사진/인쇄물 등)가 아니라 실제 매체 블록을 기준으로 결정한다.
    public enum ClueBoardNodeFormat { Card, Image, Audio, Video }

    public static class ClueBoardMediaPresentation
    {
        /// <summary>
        /// 노드가 어떤 외형을 쓸지 고른다. 눈에 보이는 매체가 대표를 맡으므로 사진 → 영상 → 소리 순이다.
        /// 매체를 여러 개 가진 단서는 대표 하나만 만질 수 있다 — 복수 매체 UX 명세가 나오면 보조 버튼으로 확장한다.
        /// </summary>
        public static ClueBoardNodeFormat ResolveFormat(ClueData clue)
        {
            if (TryGetImageAddress(clue, out _)) return ClueBoardNodeFormat.Image;
            if (TryGetVideoAddress(clue, out _)) return ClueBoardNodeFormat.Video;
            if (TryGetAudioAddress(clue, out _)) return ClueBoardNodeFormat.Audio;
            return ClueBoardNodeFormat.Card;
        }

        // 영상은 본문 매체 블록에만 있다 — ClueAttachmentKind에는 Video가 없어 첨부물은 뒤지지 않는다.
        public static bool TryGetVideoAddress(ClueData clue, out string address)
        {
            if (clue?.mediaBlocks != null)
                foreach (ClueMediaBlock block in clue.mediaBlocks)
                    if (block?.kind == ClueMediaKind.Video && !string.IsNullOrWhiteSpace(block.address)) { address = block.address; return true; }
            address = null;
            return false;
        }

        public static bool TryGetImageAddress(ClueData clue, out string address)
        {
            if (clue?.mediaBlocks != null)
                foreach (ClueMediaBlock block in clue.mediaBlocks)
                    if (block?.kind == ClueMediaKind.Image && !string.IsNullOrWhiteSpace(block.address)) { address = block.address; return true; }
            if (clue?.attachments != null)
                foreach (ClueAttachment attachment in clue.attachments)
                    if (attachment?.kind == ClueAttachmentKind.Image && !string.IsNullOrWhiteSpace(attachment.address)) { address = attachment.address; return true; }
            address = null;
            return false;
        }

        public static bool TryGetAudioAddress(ClueData clue, out string address)
        {
            if (clue?.mediaBlocks != null)
                foreach (ClueMediaBlock block in clue.mediaBlocks)
                    if (block?.kind == ClueMediaKind.Audio && !string.IsNullOrWhiteSpace(block.address)) { address = block.address; return true; }
            if (clue?.attachments != null)
                foreach (ClueAttachment attachment in clue.attachments)
                    if (attachment?.kind == ClueAttachmentKind.Audio && !string.IsNullOrWhiteSpace(attachment.address)) { address = attachment.address; return true; }
            address = null;
            return false;
        }
    }
}
