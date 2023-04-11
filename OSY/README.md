## SYComponentConverter
UI 오브젝트 ↔ Sprite 오브젝트 변환 기능 제공
  
## SYComponentsCopier
오브젝트의 모든 컨포넌트 정보를 복사, 붙여넣기 기능 제공
- Copy all components - 컨포넌트 복사
- Paste all components (Overwrite) - 붙여넣기 (덮어씌기)
- Paste all components (Except contains) - 붙여넣기 (건너뛰기)

## SYReplaceGameObjects
씬내부의 특정 이름을 가진 오브젝트를 특정 프리펩으로 바꾸어 주는 기능 제공  
![image](https://open.oss.navercorp.com/storage/user/468/files/22769d70-18ca-4ebe-b1f6-9570b8840f1b)
- Prefab - 바뀌고자하는 프리펩
- ObjectsToReplace - 바뀌는 오브젝트목록
- KeepOriginalNames - 이름 유지 여부

## MonoSpineController
해당 컨포넌트가 적용된 Spine오브젝트가 UI Spine이던 NonUISpine이던
하나의 클래스처럼 사용할 수 있도록 하는 일종의 [어댑터 클래스](https://github.com/dhtpdud/DesignPatternStudy/wiki/%EC%96%B4%EB%8C%91%ED%84%B0-%ED%8C%A8%ED%84%B4-(Adapter-pattern))
