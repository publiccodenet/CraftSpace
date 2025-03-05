<script>
  import CraftSpace from '$lib/components/CraftSpace.svelte';
  import { onMount } from 'svelte';
  import { browser } from '$app/environment';

  let unityContainer;
  let unityInstance;
  let loadingProgress = 0;
  let loadingStatus = 'Initializing...';
  let isLoaded = false;
  let initialized = false;

  onMount(() => {
    if (browser) {
      loadUnityGame();
    }
    initialized = true;
  });

  async function loadUnityGame() {
    if (!window.createUnityInstance) {
      // Unity loader script not loaded yet
      loadingStatus = 'Loading Unity framework...';
      return;
    }

    const config = {
      dataUrl: '/unity/CraftSpace.data',
      frameworkUrl: '/unity/CraftSpace.framework.js',
      codeUrl: '/unity/CraftSpace.wasm',
      streamingAssetsUrl: '/unity/StreamingAssets',
      companyName: 'Your Company',
      productName: 'CraftSpace',
      productVersion: '0.1.0'
    };

    try {
      unityInstance = await createUnityInstance(unityContainer, config, (progress) => {
        loadingProgress = Math.round(progress * 100);
        loadingStatus = `Loading game... ${loadingProgress}%`;
      });
      
      isLoaded = true;
      
      // Set up communication with Unity
      window.sendMessageToUnity = (gameObject, method, parameter) => {
        if (unityInstance) {
          unityInstance.SendMessage(gameObject, method, parameter);
        }
      };
    } catch (error) {
      console.error('Failed to load Unity WebGL application:', error);
      loadingStatus = 'Failed to load Unity content. Please check your browser supports WebGL.';
    }
  }
</script>

<svelte:head>
  <title>CraftSpace - Internet Archive 3D Explorer</title>
  <meta name="description" content="3D virtual browsing of Internet Archive collections">
  <script src="/unity/CraftSpace.loader.js"></script>
</svelte:head>

<div class="craftspace-container">
  <CraftSpace />
</div>

<main>
  {#if !isLoaded}
    <div class="loading-overlay">
      <div class="loading-container">
        <div class="loading-progress">
          <div class="progress-bar" style="width: {loadingProgress}%"></div>
        </div>
        <div class="loading-text">{loadingStatus}</div>
      </div>
    </div>
  {/if}
  
  <div id="unity-container" bind:this={unityContainer}></div>
</main>

<style>
  .craftspace-container {
    width: 100%;
    height: 100vh;
    overflow: hidden;
    position: relative;
  }
  
  :global(body) {
    margin: 0;
    padding: 0;
    overflow: hidden;
  }
  
  main {
    width: 100vw;
    height: 100vh;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    background-color: #222;
  }
  
  #unity-container {
    width: 100%;
    height: 100%;
  }
  
  .loading-overlay {
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background: rgba(0, 0, 0, 0.8);
    z-index: 100;
    display: flex;
    align-items: center;
    justify-content: center;
  }
  
  .loading-container {
    width: 60%;
    max-width: 600px;
    color: white;
    text-align: center;
    font-family: Arial, sans-serif;
  }
  
  .loading-progress {
    height: 20px;
    background-color: #333;
    border-radius: 10px;
    margin-bottom: 20px;
    overflow: hidden;
  }
  
  .progress-bar {
    height: 100%;
    background-color: #0078d7;
    transition: width 0.3s ease;
  }
  
  .loading-text {
    font-size: 16px;
  }
</style>
