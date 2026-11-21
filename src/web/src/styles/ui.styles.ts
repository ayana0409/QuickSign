// Centralized Tailwind CSS style presets for QuickSign Web Portal.
// All shared layout, card, typography, input, button, and alert styles live here
// so visual updates can be made in a single place without touching business components.

export const uiStyles = {
  layout: {
    pageCanvas:
      "relative min-h-[100dvh] bg-[#050507] text-zinc-100 antialiased selection:bg-emerald-500/20 selection:text-emerald-300",
    authSplitContainer: "min-h-[100dvh] grid grid-cols-1 lg:grid-cols-12 w-full",
    authBrandPane:
      "hidden lg:flex lg:col-span-7 flex-col justify-between p-12 xl:p-16 relative overflow-hidden border-r border-white/[0.06]",
    authFormPane:
      "col-span-1 lg:col-span-5 flex flex-col justify-between px-4 py-6 sm:px-8 sm:py-10 xl:px-14",
    authFormCenter: "mx-auto w-full max-w-md my-auto py-6",
    overviewContainer: "mx-auto max-w-4xl px-4 py-10 sm:px-6 lg:px-8 space-y-8",
    publicContainer: "mx-auto max-w-3xl px-4 py-8 sm:px-6 sm:py-12 space-y-6",
    publicHeader:
      "sticky top-0 z-20 border-b border-white/[0.06] bg-[#050507]/85 backdrop-blur-xl",
    publicHeaderInner:
      "mx-auto flex max-w-5xl items-center justify-between gap-3 px-4 py-3 sm:px-6",
    dashboardShell: "min-h-[100dvh] flex flex-col lg:flex-row",
    dashboardSidebar:
      "hidden lg:flex lg:w-64 lg:shrink-0 flex-col justify-between border-r border-white/[0.06] bg-[#07080B] p-5",
    dashboardMain: "flex-1 min-w-0 px-4 py-6 sm:px-8 sm:py-8 lg:px-10",
    dashboardContent: "mx-auto max-w-5xl space-y-6",
  },
  card: {
    doubleBezelOuter:
      "rounded-[2rem] p-1.5 bg-white/[0.03] ring-1 ring-white/[0.08] shadow-[0_24px_60px_-15px_rgba(0,0,0,0.7)]",
    doubleBezelInner:
      "rounded-[calc(2rem-0.375rem)] bg-[#0B0C10] p-6 sm:p-8 shadow-[inset_0_1px_1px_rgba(255,255,255,0.12)]",
    metricTile:
      "rounded-2xl bg-white/[0.025] p-4 ring-1 ring-white/[0.07] transition-colors duration-300 hover:bg-white/[0.04]",
  },
  typography: {
    eyebrowBadge:
      "inline-flex items-center gap-2 rounded-full px-3 py-1 text-[10px] uppercase tracking-[0.2em] font-medium bg-white/[0.04] text-emerald-400 ring-1 ring-emerald-500/20",
    heroHeading:
      "text-3xl sm:text-4xl xl:text-5xl font-semibold tracking-tight text-white leading-[1.12]",
    sectionTitle: "text-xl sm:text-2xl font-semibold tracking-tight text-white",
    bodyMuted: "text-sm text-zinc-400 leading-relaxed",
    monoCode:
      "font-mono text-xs text-zinc-300 bg-white/[0.04] px-2 py-0.5 rounded-md ring-1 ring-white/10",
  },
  input: {
    wrapper: "space-y-1.5",
    label: "block text-xs font-medium uppercase tracking-wider text-zinc-400",
    fieldContainer: "relative flex items-center",
    fieldBase:
      "min-h-[44px] w-full rounded-2xl bg-white/[0.03] px-4 py-3 text-sm text-white placeholder:text-zinc-600 ring-1 ring-white/10 transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)] focus:bg-white/[0.05] focus:outline-none focus:ring-2 focus:ring-emerald-500/60",
    fieldWithTrailingIcon: "pr-12",
    fieldError: "ring-rose-500/60 focus:ring-rose-500",
    trailingToggleBtn:
      "absolute right-1.5 flex h-9 w-9 min-h-[44px] min-w-[44px] items-center justify-center rounded-xl text-zinc-400 transition-colors hover:text-zinc-200 focus:outline-none focus:text-white",
    errorText: "text-xs text-rose-400 mt-1 flex items-center gap-1.5",
  },
  button: {
    islandPrimary:
      "group relative inline-flex min-h-[48px] w-full items-center justify-between rounded-full bg-white px-6 py-2.5 text-sm font-medium text-zinc-950 transition-all duration-500 ease-[cubic-bezier(0.32,0.72,0,1)] hover:bg-zinc-100 active:scale-[0.98] disabled:opacity-50 disabled:pointer-events-none cursor-pointer",
    islandIconWrap:
      "ml-3 flex h-8 w-8 items-center justify-center rounded-full bg-zinc-950/10 transition-transform duration-500 ease-[cubic-bezier(0.32,0.72,0,1)] group-hover:translate-x-1 group-hover:-translate-y-[1px] group-hover:scale-105",
    ghostPill:
      "inline-flex min-h-[44px] items-center justify-center gap-2 rounded-full bg-white/[0.04] px-4 py-2 text-xs font-medium text-zinc-300 ring-1 ring-white/10 transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)] hover:bg-white/[0.08] hover:text-white active:scale-[0.98] cursor-pointer",
    verifyCtaPill:
      "inline-flex min-h-[44px] items-center justify-center gap-2 rounded-full bg-emerald-500/15 px-4 py-2 text-xs font-semibold text-emerald-300 ring-1 ring-emerald-500/40 transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)] hover:bg-emerald-500/25 hover:text-emerald-200 active:scale-[0.98] cursor-pointer",
    verifyBannerLink:
      "group flex min-h-[48px] w-full items-center justify-between rounded-2xl bg-emerald-500/[0.08] px-4 py-3 text-sm font-medium text-emerald-300 ring-1 ring-emerald-500/30 transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)] hover:bg-emerald-500/[0.15] hover:ring-emerald-400/50 active:scale-[0.99]",
    dangerPill:
      "inline-flex min-h-[44px] items-center justify-center gap-2 rounded-full bg-rose-500/10 px-4 py-2 text-xs font-medium text-rose-300 ring-1 ring-rose-500/25 transition-all duration-300 hover:bg-rose-500/20 active:scale-[0.98] cursor-pointer",
  },
  alert: {
    success:
      "rounded-2xl bg-emerald-500/10 p-4 text-xs sm:text-sm text-emerald-300 ring-1 ring-emerald-500/25 flex items-start gap-3",
    error:
      "rounded-2xl bg-rose-500/10 p-4 text-xs sm:text-sm text-rose-300 ring-1 ring-rose-500/25 flex items-start gap-3",
    warning:
      "rounded-2xl bg-amber-500/10 p-4 text-xs sm:text-sm text-amber-300 ring-1 ring-amber-500/25 flex items-start gap-3",
  },
  badge: {
    emerald:
      "inline-flex items-center gap-1.5 rounded-full bg-emerald-500/10 px-2.5 py-1 text-[11px] font-medium text-emerald-300 ring-1 ring-emerald-500/20",
    amber:
      "inline-flex items-center gap-1.5 rounded-full bg-amber-500/10 px-2.5 py-1 text-[11px] font-medium text-amber-300 ring-1 ring-amber-500/20",
    zinc:
      "inline-flex items-center gap-1.5 rounded-full bg-white/[0.05] px-2.5 py-1 text-[11px] font-medium text-zinc-300 ring-1 ring-white/10",
    danger:
      "inline-flex items-center gap-1.5 rounded-full bg-rose-500/10 px-2.5 py-1 text-[11px] font-medium text-rose-300 ring-1 ring-rose-500/20",
  },
  tabs: {
    list: "inline-flex w-full rounded-2xl bg-white/[0.03] p-1 ring-1 ring-white/[0.08]",
    tabBase:
      "flex-1 min-h-[44px] rounded-xl px-4 py-2 text-xs sm:text-sm font-medium transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)] cursor-pointer focus:outline-none focus-visible:ring-2 focus-visible:ring-emerald-500/60",
    tabActive: "bg-white text-zinc-950 shadow-sm",
    tabInactive: "text-zinc-400 hover:text-white",
  },
  dropzone: {
    base: "group relative flex min-h-[190px] w-full cursor-pointer flex-col items-center justify-center gap-3 rounded-2xl border border-dashed border-white/15 bg-white/[0.02] p-6 text-center transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)] hover:border-emerald-400/50 hover:bg-white/[0.04] focus-within:ring-2 focus-within:ring-emerald-500/60",
    dragging: "border-emerald-400 bg-emerald-500/[0.06]",
    iconWrap:
      "flex h-12 w-12 items-center justify-center rounded-2xl bg-white/[0.04] text-emerald-400 ring-1 ring-white/10 transition-transform duration-300 group-hover:scale-105",
  },
  resultCard: {
    valid: "rounded-2xl bg-emerald-500/[0.07] p-5 ring-1 ring-emerald-500/25 space-y-4",
    warning: "rounded-2xl bg-amber-500/[0.07] p-5 ring-1 ring-amber-500/25 space-y-4",
    danger: "rounded-2xl bg-rose-500/[0.07] p-5 ring-1 ring-rose-500/25 space-y-4",
    neutral: "rounded-2xl bg-white/[0.03] p-5 ring-1 ring-white/10 space-y-4",
  },
  detailsDrawer: {
    root: "rounded-2xl bg-white/[0.02] ring-1 ring-white/[0.07] overflow-hidden",
    summary:
      "flex min-h-[44px] cursor-pointer items-center justify-between px-4 py-2.5 text-xs font-medium text-zinc-300 select-none hover:bg-white/[0.03] focus:outline-none focus-visible:ring-2 focus-visible:ring-emerald-500/60",
    body: "border-t border-white/[0.06] px-4 py-3 space-y-2.5",
  },
  nav: {
    itemBase:
      "flex min-h-[44px] items-center gap-3 rounded-xl px-3.5 py-2.5 text-sm font-medium transition-colors duration-200",
    itemActive: "bg-emerald-500/15 text-emerald-300 ring-1 ring-emerald-500/25",
    itemInactive: "text-zinc-400 hover:bg-white/[0.04] hover:text-white",
  },
  table: {
    wrapper: "hidden md:block overflow-x-auto rounded-2xl ring-1 ring-white/[0.08] bg-[#0B0C10]",
    table: "w-full text-left text-sm",
    thead: "border-b border-white/[0.06] text-[11px] uppercase tracking-wider text-zinc-500",
    th: "px-4 py-3.5 font-medium",
    tr: "border-b border-white/[0.04] last:border-none hover:bg-white/[0.02] transition-colors",
    td: "px-4 py-3.5 align-middle",
  },
  modal: {
    dialog:
      "m-auto w-[calc(100%-2rem)] max-w-lg rounded-[2rem] bg-transparent p-0 text-zinc-100 backdrop:bg-black/75 backdrop:backdrop-blur-sm open:animate-in",
  },
} as const;
