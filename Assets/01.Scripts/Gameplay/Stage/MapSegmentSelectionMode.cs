namespace Game.Gameplay.Stage
{
    /// <summary>다음 맵 세그먼트를 선택하는 방식을 정의합니다.</summary>
    public enum MapSegmentSelectionMode
    {
        /// <summary>후보 중 하나를 난수로 선택합니다.</summary>
        Random,

        /// <summary>후보 배열에 입력된 순서를 반복합니다.</summary>
        Sequence
    }
}
