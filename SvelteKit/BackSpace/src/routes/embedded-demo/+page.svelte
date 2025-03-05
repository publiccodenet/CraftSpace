<script>
  import CraftSpace from '$lib/components/CraftSpace.svelte';
</script>

<div class="page-container">
  <h1>CraftSpace Embedded Demo</h1>
  
  <div class="craft-space-container">
    <CraftSpace 
      width="800px"
      height="600px"
      fullPage={false}
    />
  </div>
  
  <div class="description">
    <h2>About CraftSpace</h2>
    <p>CraftSpace is an interactive 3D environment for exploring digital archives.</p>
    <p>This embedded demo shows how CraftSpace can be integrated into any page layout.</p>
  </div>
</div>

<style>
  .page-container {
    max-width: 1200px;
    margin: 0 auto;
    padding: 2rem;
  }
  
  .craft-space-container {
    margin: 2rem 0;
    border: 1px solid #ccc;
    border-radius: 8px;
    overflow: hidden;
  }
  
  .description {
    margin-top: 2rem;
  }
</style> 