import { GridItemHTMLElement, GridStack, GridStackElement, GridStackOptions } from "gridstack";

function init(
  options: GridStackOptions = {},
  elOrString: GridStackElement = ".grid-stack",
  dotNet: DotNet.DotNetObject
) {
  const grid = GridStack.init(options, elOrString);
  grid["dotNet"] = dotNet;
  addEvents(grid);
  return grid;
}

function setStatic(grid: GridStack, staticValue: boolean) {
  if (grid) {
    grid.setStatic(staticValue);
  }
}

function reload(grid: GridStack) {
  if (grid) {
    const opts = { ...grid.opts };
    const el = grid.el;
    grid.destroy(false);
    const dotNet = grid["dotNet"];
    grid = GridStack.init(opts, el);
    grid["dotNet"] = dotNet;
    addEvents(grid);
    return grid;
  }

  return grid;
}

function save(grid: GridStack) {
  if (grid) {
    const widgets = grid.save();
    if (Array.isArray(widgets)) {
      return widgets.map(({ content, ...rest }) => rest);
    }
  }

  return [];
}

function addEvents(grid: GridStack) {
  if (!grid) return;

  const dotNet: DotNet.DotNetObject = grid["dotNet"];
  grid.on("resize", function (event: Event, el: GridItemHTMLElement) {
    dotNet.invokeMethodAsync("OnResize", ...resize(event, el));
  });
}

function resize(event: Event, el: GridItemHTMLElement) {
  const customElement = el.firstElementChild.firstElementChild;
  let id;
  let width = 0;
  let height = 0;
  if (customElement) {
    id = el.getAttribute("gs-id");
    width = customElement.clientWidth;
    height = customElement.clientHeight;
  }
  return [id, width, height];
}

export { init, reload, setStatic, save };
