## [SYComponentConverter](https://github.com/dhtpdud/MyWorks/blob/main/SYComponentConverter.cs)
UI 오브젝트 ↔ Sprite 오브젝트 변환 기능 제공

## [MonoSpineController](https://github.com/dhtpdud/MyWorks/blob/main/MonoSpineController.cs)
해당 컴포넌트가 적용된 Spine오브젝트가 UI Spine이던 NonUISpine이던  
하나의 클래스처럼 사용할 수 있도록 하는 일종의 [어댑터 클래스](https://github.com/dhtpdud/DesignPatternStudy/wiki/%EC%96%B4%EB%8C%91%ED%84%B0-%ED%8C%A8%ED%84%B4-(Adapter-pattern))

## [SYMonoSpineController](https://github.com/dhtpdud/MyWorks/blob/main/SYMonoSpineController.cs)
Spine을 Animator처럼 사용하기위한 FSM

## [SYAudioSpectrum](https://github.com/dhtpdud/MyWorks/blob/main/SYAudioSpectrum.cs), [SYAudioVisualizer](https://github.com/dhtpdud/MyWorks/blob/main/SYAudioVisualizer.cs)
SYAudioSpectrum: AudioSource 에서 오디오 스펙트럼 값을 추출하여 float 배열에 저장해둠  
SYAudioVisualizer: SYAudioSpectrum객체에 저장되어있는 스펙트럼값을 이용하여 원하는 오브젝트의 크기를 변형시킴

## [SYInfiniteFlowSlider](https://github.com/dhtpdud/MyWorks/blob/main/SYInfiniteFlowSlider.cs)
원하는 오브젝트를 특정 방향으로 계속 움직이게하고, 특정 위치를 벗어나면 위치값을 다시 초기화 하는 기능 제공
DoTween 사용

## [SYRigidbodyController](https://github.com/dhtpdud/MyWorks/blob/main/SYRigidbodyController.cs)
Rigidbody 컴포넌트가 적용되어있는 오브젝트들이
모바일의 자이로 센서에 따라 햅틱반응을 할 수 있도록 관리하는 컴포넌트 <sup>[옵저버 패턴](https://github.com/dhtpdud/DesignPatternStudy/wiki/%EC%98%B5%EC%A0%80%EB%B2%84-%ED%8C%A8%ED%84%B4-(Observer-pattern))</sup>

## [SYTrakingMeshUI](https://github.com/dhtpdud/MyWorks/blob/main/SYTrakingMeshUI.cs)
UI의 Mesh를 특정 오브젝트의 위치에 따라 넓히거나 줄일 수 있는 기능 제공  
크기가 유동적인 UI마스크를 만들고 싶을때 사용하면 유용  
예시)  
![예제2](https://user-images.githubusercontent.com/1351568/231068612-d92f6bbf-350a-42e9-8033-f2e6a16ca439.gif)

## [SYTransformSynchronizer](https://github.com/dhtpdud/MyWorks/blob/main/SYTransformSynchronizer.cs)
단순 위치 동기화

## [SYUtil](https://github.com/dhtpdud/MyWorks/blob/main/SYUtil.cs)
백터 각도, 효과음 재생, DelayCall, 코루틴에서 사용되는 YieldInstructionCache 등등  
어느 클래스에서든 활용할 수 있는 코드 모음 <sup>[퍼사드 패턴](https://github.com/dhtpdud/DesignPatternStudy/wiki/%ED%8D%BC%EC%82%AC%EB%93%9C-%ED%8C%A8%ED%84%B4-(Facade-pattern))</sup>  
