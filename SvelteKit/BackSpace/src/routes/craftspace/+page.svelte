<script>
  import CraftSpace from '$lib/components/CraftSpace.svelte';
  
  // Optional: Additional configuration or state
  let craftSpaceComponent;
  
  // Example of interacting with the Unity instance
  function sendCommandToUnity() {
    if (craftSpaceComponent) {
      craftSpaceComponent.sendMessage('GameController', 'PerformAction', 'some-action');
    }
  }
</script>

<CraftSpace 
  bind:this={craftSpaceComponent}
  fullPage={true}
  backgroundColor="#1a1a1a"
/>

<style>
  :global(body) {
    margin: 0;
    padding: 0;
    overflow: hidden;
  }
</style> 