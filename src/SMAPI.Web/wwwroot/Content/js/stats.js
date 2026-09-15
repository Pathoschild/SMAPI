var smapi = smapi || {};
var app;
smapi.statsPage = function (options) {
    /*********
    ** Configure
    *********/
    const config = {
        // default colors for chart series (which cycle through the list)
        colorPalette: [
            "#117733", // dark green
            "#332288", // dark indigo
            "#88ccee", // azure
            "#cc6677", // muted rose
            "#ddcc77", // gold
            "#44aa99", // turquoise
            "#aa4499"  // magenta
        ],

        // configure the 'mods by type' stats
        modsByType: {
            // display names for each mod type (omitted mod types are shown as-is)
            labels: {
                // keys must be lowercase
                "smapi": "SMAPI (C#)",
                "pathoschild.contentpatcher": "Content Patcher",
                "xnb": "XNB",
                "peacefulend.alternativetextures": "Alternative Textures",
                "spacechase0.jsonassets": "Json Assets",
                "peacefulend.fashionsense": "Fashion Sense",
                "digus.producerframeworkmod": "Producer Framework Mod",
                "digus.mailframeworkmod": "Mail Framework Mod",
                "esca.farmtypemanager": "Farm Type Manager",
                "cherry.shoptileframework": "Shop Tile Framework",
                "platonymous.tmxloader": "TMXL",
                "cat.betterartisangoodicons": "Better Artisan Good Icons",
                "spacechase0.dynamicgameassets": "Dynamic Game Assets",
                "platonymous.customfurniture": "Custom Furniture",
                "platonymous.custommusic": "Custom Music",
                "leroymilo.furnitureframework": "Furniture Framework",
                "paritee.betterfarmanimalvariety": "Better Farm Animal Variety",
                "other": "other (<100 packs)"
            },

            // colors for each mod type (omitted mod types cycle through the colorPalette)
            colors: {
                "other": "#c0c0c8" // silver-gray
            },

            // notable events which may affect stats, indexed by their date in mods-by-type.jsonl
            notableEvents: {
                "2021-01-01": "Stardew Valley 1.5",   // 21 December 2020
                "2021-09-01": "Stardew Valley 1.5.5", // 17 August 2021
                "2024-04-01": "Stardew Valley 1.6",   // 19 March 2024
                "2024-10-31": "Stardew Valley 1.6.9"  // 04 November 2024
            }
        },

        // configure the 'content packs by format version' stats
        contentPacks: {
            // colors for each format version (omitted entries cycle through the colorPalette)
            colors: {},

            // notable events which may affect stats, indexed by their date in content-packs-by-format.jsonl
            notableEvents: {
                "2021-08-01": "Stardew Valley 1.5.5", // 17 August 2021
                "2024-04-01": "Stardew Valley 1.6",   // 19 March 2024
                "2024-10-31": "Stardew Valley 1.6.9"  // 04 November 2024
            }
        },

        // configure the 'web traffic' stats
        dnsQueries: {
            // notable events which may affect traffic, indexed by their date in smapi-dns-queries.json
            notableEvents: {
                "2018-05": "Stardew Valley 1.3 beta", // 30 April 2018
                "2018-08": "Stardew Valley 1.3",      // 01 August 2018
                "2019-12": "Stardew Valley 1.4",      // 26 November 2019
                "2020-12": "Stardew Valley 1.5",      // 21 December 2020
                "2021-08": "Stardew Valley 1.5.5",    // 17 August 2021
                "2024-03": "Stardew Valley 1.6",      // 19 March 2024
                "2024-11": "Stardew Valley 1.6.9"     // 04 November 2024
            }
        },

        // configure the 'SMAPI costs' stats
        costs: {
            // colors for each service (omitted entries cycle through the colorPalette)
            colors: {
                // keys must be lowercase
                "github": "#ddcc77",         // gold
                "mongodb": "#aa4499",        // magenta
                "azure hosting": "#db4437",  // muted rose
                "amazon hosting": "#332288", // dark indigo
                "amazon ssl": "#44aa99",     // turquoise
                "amazon domains": "#117733", // dark green
                "malware scan": "#88ccee"    // azure
            },

            // notable events which may affect costs, indexed by their date in smapi-costs.jsonl
            notableEvents: {
                "2019-12": "Stardew Valley 1.4",  // 26 November 2019
                "2020-12": "Stardew Valley 1.5",  // 21 December 2020
                "2024-03": "Stardew Valley 1.6",  // 19 March 2024
                "2024-11": "Stardew Valley 1.6.9" // 04 November 2024
            }
        }
    };


    /*********
    ** Set up state
    *********/
    /**
     * The internal data loaded from the dataset files, used to build the charts.
     */
    const data = {
        /**
         * The data loaded from `mods-by-type.jsonl`.
         */
        modsByType: {
            rows: [],
            lastRow: {},
            xLabels: [],
            modTypes: [],

            newMods: {
                deltas: [],
                xLabels: []
            }
        },

        /**
         * The data loaded from `content-packs-by-format.jsonl`.
         */
        contentPacks: {
            rows: [],
            lastRow: {},
            xLabels: [],
            versions: []
        },

        /**
         * The data loaded from `smapi-dns-queries.json`.
         */
        dnsQueries: {
            xLabels: [],
            values: []
        },

        /**
         * The data loaded from `smapi-costs.jsonl`.
         */
        costs: {
            rows: [],
            serviceKeys: [],
            xLabels: []
        }
    };

    /**
     * The generated chart instances.
     */
    const charts = [];


    /*********
    ** Init app
    *********/
    app = new Vue({
        el: "#app",
        data: {
            /**
             * Whether the data files are currently being fetched, so the stats shouldn't be shown yet.
             */
            isLoadingData: true,

            /**
             * Whether stats for at least one data file were loaded successfully.
             */
            anyDataLoaded: false,

            /**
             * View data based on `mods-by-type.jsonl`.
             */
            modsByType: {
                loadFailed: false,

                previouslyUpdated: null,
                lastUpdated: null,
                total: 0,
                topThree: [],
                xnbPercent: "0",

                latestNewMods: {
                    total: 0,
                    byType: [],
                    forOther: 0
                }
            },

            /**
             * View data based on `content-packs-by-format.jsonl`.
             */
            contentPacks: {
                loadFailed: false,

                previouslyUpdated: null,
                lastUpdated: null,
                total: 0,
                topVersions: []
            },

            /**
             * View data based on `smapi-dns-queries.json`.
             */
            dnsQueries: {
                loadFailed: false,

                lastUpdated: null,
                latestCount: 0
            },

            /**
             * View data based on `smapi-costs.jsonl`.
             */
            costs: {
                loadFailed: false,

                lastUpdated: null,
                previousCost: 0,
                latestCost: 0,
                percentChange: 0
            }
        },
        watch: {
            isLoadingData: function () {
                const quickNav = document.getElementById("quickNav");
                if (quickNav)
                    quickNav.hidden = this.isLoadingData;
            }
        },
        mounted: function () {
            this.loadAsync();
        },
        methods: {
            /**
             * Fetch the raw data files, initialize the dataset, and build the page's charts.
             */
            loadAsync: async function () {
                await this.loadDataAsync();
                await this.createChartsAsync();

                window.addEventListener("resize", () => resizeCharts(charts));
            },

            /**
             * Fetch the raw data files and initialize the dataset.
             */
            loadDataAsync: async function () {
                /*********
                ** Fetch data files
                *********/
                const [modsByTypeRows, contentPacksRows, costsRows, dnsData] = await Promise.all([
                    fetchJsonLinesAsync(options.modsByTypeUri),
                    fetchJsonLinesAsync(options.contentPacksByFormatUri),
                    fetchJsonLinesAsync(options.smapiCostsUri),
                    fetchJsonAsync(options.smapiDnsQueriesUri)
                ]);


                /*********
                ** Process mods by type
                *********/
                this.modsByType.loadFailed = true; // override on success
                if (modsByTypeRows) {
                    try {
                        const rows = modsByTypeRows.map(lowercaseKeys);

                        // parse data
                        const lastRow = rows[rows.length - 1];
                        data.modsByType = {
                            previouslyUpdated: formatFullDate(rows[rows.length - 2].date),
                            lastUpdated: formatFullDate(lastRow.date),

                            rows,
                            lastRow,
                            xLabels: rows.map(row => row.date),
                            modTypes: getModKeys(rows)
                        };
                        this.modsByType.previouslyUpdated = data.modsByType.previouslyUpdated;
                        this.modsByType.lastUpdated = data.modsByType.lastUpdated;

                        // assign colors for each mod type
                        assignColors(config.modsByType.colors, data.modsByType.modTypes);

                        // derive high-level summary
                        {
                            const curData = data.modsByType;
                            const total = getSum(curData.modTypes, modType => curData.lastRow[modType]);

                            const topThree = curData.modTypes
                                .filter(isSpecificModType)
                                .slice(0, 3)
                                .map(modType => ({ label: getLabel(config.modsByType.labels, modType), percent: getPercent(curData.lastRow[modType], total) }));

                            this.modsByType.total = total;
                            this.modsByType.topThree = topThree;
                            this.modsByType.xnbPercent = getPercent(curData.lastRow["xnb"], total);
                        }

                        // derive 'new mods this month' stats
                        {
                            const curData = data.modsByType;

                            // exclude historical manual adjustments (e.g. from mod dump rebuilds)
                            const excludeDates = new Set(["2021-03-06", "2022-01-01", "2023-05-04", "2024-08-02"]);
                            const excludeRow = row => excludeDates.has(row.date);

                            // get total new mods per month (skip first row which has no delta)
                            const totals = curData.rows.map(row => getSum(curData.modTypes, modType => row[modType]));
                            const deltas = curData.rows
                                .slice(1)
                                .map((_, i) => totals[i + 1] - totals[i])
                                .filter((_, i) => !excludeRow(curData.rows[i + 1]));
                            const filteredXLabels = curData.xLabels
                                .slice(1)
                                .filter((_, i) => !excludeRow(curData.rows[i + 1]));
                            const lastDelta = deltas[deltas.length - 1];

                            // get per-type deltas for last month
                            const lastRow = curData.lastRow;
                            const prevRow = curData.rows[curData.rows.length - 2];
                            const byType = curData.modTypes
                                .filter(isSpecificModType)
                                .map(modType => ({
                                    type: modType,
                                    label: getLabel(config.modsByType.labels, modType),
                                    delta: (lastRow[modType] ?? 0) - (prevRow[modType] ?? 0)
                                }))
                                .sort((a, b) => b.delta - a.delta);
                            const otherDelta = (lastRow["other"] ?? 0) - (prevRow["other"] ?? 0);

                            // save values
                            data.modsByType.newMods = {
                                deltas: deltas,
                                xLabels: filteredXLabels
                            };
                            this.modsByType.latestNewMods = {
                                total: lastDelta,
                                byType: byType,
                                forOther: otherDelta
                            };
                        }

                        this.modsByType.loadFailed = false;
                        this.anyDataLoaded = true;
                    }
                    catch (error) {
                        console.error(error);
                    }
                }


                /*********
                ** Process content packs by format version
                *********/
                this.contentPacks.loadFailed = true; // override on success
                if (contentPacksRows) {
                    try {
                        // parse data
                        const rows = contentPacksRows;
                        const lastRow = rows[rows.length - 1];
                        const prevRow = rows[rows.length - 2];
                        const versions = getModKeys(rows);
                        data.contentPacks = {
                            rows,
                            lastRow,
                            xLabels: rows.map(row => row.date),
                            versions
                        };

                        // assign colors for each format version
                        assignColors(config.contentPacks.colors, versions);

                        // derive summary of top versions
                        const total = getSum(versions, version => lastRow[version]);
                        this.contentPacks = {
                            loadFailed: false,

                            previouslyUpdated: formatFullDate(prevRow.date),
                            lastUpdated: formatFullDate(lastRow.date),
                            total,
                            topVersions: versions.slice(0, 5).map(version => ({
                                version,
                                count: lastRow[version] ?? 0,
                                delta: (lastRow[version] ?? 0) - (prevRow[version] ?? 0)
                            }))
                        };

                        this.anyDataLoaded = true;
                    }
                    catch (error) {
                        console.error(error);
                    }
                }


                /*********
                ** Process DNS queries
                *********/
                this.dnsQueries.loadFailed = true; // override on success
                if (dnsData) {
                    try {
                        const xLabels = Object.keys(dnsData);
                        const values = Object.values(dnsData);

                        data.dnsQueries = {
                            xLabels,
                            values
                        };

                        this.dnsQueries = {
                            loadFailed: false,

                            lastUpdated: formatMonthYear(xLabels[xLabels.length - 1]),
                            latestCount: values[values.length - 1]
                        };

                        this.anyDataLoaded = true;
                    }
                    catch (error) {
                        console.error(error);
                    }
                }


                /*********
                ** Process SMAPI costs
                *********/
                this.costs.loadFailed = true; // override on success
                if (costsRows) {
                    try {
                        // parse data
                        const rows = [...costsRows]; // copy since we'll be mutating it
                        const serviceKeys = [...new Set(rows.flatMap(row => Object.keys(row).filter(isDataField)))];
                        const xLabels = rows.map(row => row.date);

                        // amortize annual costs into monthly ones (so graph is more representative of average monthly costs)
                        {
                            const amortizing = [];
                            for (const row of rows) {
                                // collect new amortized amounts
                                for (const [key, value] of Object.entries(row)) {
                                    if (value.amount && value.months) {
                                        amortizing.push({ key, perMonth: value.amount / value.months, months: value.months });
                                        row[key] = 0;
                                    }
                                }

                                // apply amortized amounts
                                for (let i = amortizing.length - 1; i >= 0; i--) {
                                    const entry = amortizing[i];
                                    row[entry.key] = (row[entry.key] ?? 0) + entry.perMonth;
                                    entry.months--;

                                    if (entry.months < 1)
                                        amortizing.splice(i, 1);
                                }
                            }
                        }

                        // sort by descending cost in the last row (so the largest segment is at the bottom of the stacked bar)
                        serviceKeys.sort((a, b) => (rows[rows.length - 1][b] ?? 0) - (rows[rows.length - 1][a] ?? 0));

                        // set data
                        assignColors(config.costs.colors, serviceKeys);
                        data.costs = { rows, serviceKeys, xLabels };

                        // set summary
                        const prevRow = rows[rows.length - 2];
                        const lastRow = rows[rows.length - 1];
                        const previousCost = Math.round(getSum(serviceKeys, key => prevRow[key]) * 100) / 100;
                        const latestCost = Math.round(getSum(serviceKeys, key => lastRow[key]) * 100) / 100;
                        this.costs = {
                            loadFailed: false,

                            lastUpdated: formatMonthYear(rows[rows.length - 1].date),
                            previousCost,
                            latestCost,
                            percentChange: Math.abs((latestCost - previousCost) / previousCost)
                        };

                        this.anyDataLoaded = true;
                    }
                    catch (error) {
                        console.error(error);
                    }
                }


                /*********
                ** Mark data ready
                *********/
                this.isLoadingData = false;
            },

            /**
             * Create (or recreate) the charts on the page.
             */
            createChartsAsync: async function () {
                /*********
                ** Reset
                *********/
                // destroy any current charts
                for (const chart of charts)
                    chart.dispose();
                charts.length = 0;

                // wait for Vue to render chart containers if needed
                await this.$nextTick();


                /*********
                ** Add charts based on mods-by-type.jsonl
                *********/
                if (!this.modsByType.loadFailed) {
                    try {
                        const curData = data.modsByType;
                        const lastRowIndex = curData.rows.length - 1;

                        // 'mods by type' pie chart
                        {
                            const chart = echarts.init(document.getElementById("modsByType"));

                            chart.setOption({
                                baseOption: {
                                    series: [createPieChartSeries()],
                                    timeline: createTimeline(curData.xLabels, config.modsByType.notableEvents),
                                    toolbox: createToolbox(false)
                                },
                                options: curData.rows.map((row, i) => {
                                    const isLatest = i === lastRowIndex;
                                    const total = getSum(curData.modTypes, modType => row[modType]);

                                    return {
                                        title: {
                                            text: getTimelineTitle("Total mods by type", row.date, isLatest),
                                            textStyle: createTitleStyle()
                                        },
                                        series: [
                                            {
                                                label: {
                                                    formatter: entry => `${entry.name}  ${getPercent(entry.value, total)}%`
                                                },
                                                data: curData.modTypes.map(modType => ({
                                                    name: getLabel(config.modsByType.labels, modType),
                                                    value: row[modType] ?? 0,
                                                    label: {
                                                        show: !!row[modType]
                                                    },
                                                    itemStyle: {
                                                        color: getColor(config.modsByType.colors, modType)
                                                    }
                                                }))
                                            }
                                        ],
                                        tooltip: {
                                            formatter: entry => entry.name && entry.value
                                                ? `${entry.name}: ${entry.value.toLocaleString()} (${getPercent(entry.value, total)}%)`
                                                : null // not a slice (e.g. a timeline control)
                                        },
                                        animation: isLatest // don't animate previous rows, which prevents the chart from updating while playing/scrolling timeline
                                    };
                                })
                            });

                            charts.push(chart);
                        }

                        // 'total mods by type' line charts
                        {
                            const chartConfigs = [
                                {
                                    id: "modsByTypeOverTime",
                                    title: "Mods by type over time",
                                    modTypes: curData.modTypes.filter(isSpecificModType)
                                },
                                {
                                    id: "modsByTypeOverTimeExcludingOutliers",
                                    title: "Mods by type over time (excluding SMAPI and Content Patcher)",
                                    modTypes: curData.modTypes.filter(key => isSpecificModType(key) && key !== "smapi" && key !== "pathoschild.contentpatcher")
                                }
                            ];

                            for (const chartConfig of chartConfigs) {
                                const chart = echarts.init(document.getElementById(chartConfig.id));

                                chart.setOption({
                                    title: {
                                        text: chartConfig.title,
                                        textStyle: createTitleStyle()
                                    },
                                    legend: {
                                        show: false
                                    },
                                    grid: {
                                        top: 40,
                                        left: 60,
                                        right: 150 // make room for end labels
                                    },
                                    xAxis: {
                                        data: curData.xLabels,
                                        axisLabel: {
                                            rotate: 90,
                                            fontSize: 11,
                                            formatter: value => value.slice(0, 7)
                                        }
                                    },
                                    yAxis: {},
                                    series: addMarkLine(
                                        chartConfig.modTypes.map(modType => {
                                            const extendsToEnd = !!curData.lastRow[modType];
                                            const rawColor = getColor(config.modsByType.colors, modType);
                                            const color = extendsToEnd
                                                ? rawColor
                                                : hexToRgba(rawColor, 0.6);

                                            return {
                                                type: "line",
                                                name: getLabel(config.modsByType.labels, modType),
                                                data: curData.rows.map(row => row[modType] ?? null),
                                                color: color,
                                                lineStyle: {
                                                    type: extendsToEnd ? "solid" : "dotted",
                                                    width: 2
                                                },
                                                symbol: "none",
                                                endLabel: {
                                                    show: extendsToEnd,
                                                    formatter: "{a}", // {a} = name
                                                    color: color
                                                },
                                                labelLayout: {
                                                    moveOverlap: extendsToEnd
                                                        ? "shiftY" // if the line reaches the end of the chart, use 'shiftY' layout to avoid overlapping labels
                                                        : null     // if the line ends early, just display it next to the line instead
                                                }
                                            };
                                        }),
                                        p => p.name,
                                        config.modsByType.notableEvents
                                    ),
                                    tooltip: {
                                        trigger: "axis"
                                    },
                                    toolbox: createToolbox(true)
                                });

                                charts.push(chart);
                            }
                        }

                        // 'new mods' pie chart
                        {
                            const chart = echarts.init(document.getElementById("newModsLastDelta"));

                            chart.setOption({
                                baseOption: {
                                    series: [
                                        {
                                            ...createPieChartSeries(),
                                            label: {
                                                formatter: entry => `${entry.name}  ${entry.percent.toFixed(1)}%`
                                            },
                                            data: [] // overridden per-frame in `options`
                                        }
                                    ],
                                    tooltip: {
                                        formatter: entry => `${entry.name}: ${entry.value.toLocaleString()} (${entry.percent.toFixed(1)}%)`
                                    },
                                    toolbox: createToolbox(false),
                                    timeline: createTimeline(
                                        curData.xLabels.slice(1),
                                        config.modsByType.notableEvents,
                                        {
                                            controlStyle: {
                                                showPlayBtn: false // hide play button, since the data is too erratic for autoplay to be useful
                                            }
                                        }
                                    )
                                },
                                options: curData.rows.slice(1).map((row, i) => {
                                    const prevRow = curData.rows[i];
                                    const isLatest = i === lastRowIndex - 1; // rendered rows start at row 1 (since they delta against previous row)

                                    return {
                                        title: {
                                            text: `New mods between ${formatFullDate(prevRow.date)} and ${formatFullDate(row.date)}`,
                                            textStyle: createTitleStyle()
                                        },
                                        series: [
                                            {
                                                data: curData.modTypes
                                                    .map(modType => ({
                                                        type: modType,
                                                        delta: (row[modType] ?? 0) - (prevRow[modType] ?? 0)
                                                    }))
                                                    .filter(entry => entry.delta > 0)
                                                    .map(entry => ({
                                                        name: getLabel(config.modsByType.labels, entry.type),
                                                        value: entry.delta,
                                                        itemStyle: {
                                                            color: getColor(config.modsByType.colors, entry.type)
                                                        }
                                                    }))
                                            }
                                        ],
                                        animation: isLatest
                                    };
                                })
                            });

                            charts.push(chart);
                        }

                        // 'new mods' bar chart
                        {
                            const chart = echarts.init(document.getElementById("newMods"));

                            chart.setOption({
                                title: {
                                    text: "New mods by month",
                                    textStyle: createTitleStyle()
                                },
                                legend: {
                                    show: false
                                },
                                grid: {
                                    top: 40,
                                    left: 60,
                                    right: 60
                                },
                                xAxis: {
                                    data: data.modsByType.newMods.xLabels,
                                    axisLabel: {
                                        rotate: 90,
                                        fontSize: 11,
                                        formatter: value => value.slice(0, 7)
                                    }
                                },
                                yAxis: {},
                                series: [
                                    {
                                        type: "bar",
                                        data: data.modsByType.newMods.deltas,
                                        markLine: createMarkLine(p => p.name, config.modsByType.notableEvents)
                                    }
                                ],
                                tooltip: {
                                    trigger: "axis"
                                },
                                toolbox: createToolbox(true)
                            });
                            charts.push(chart);
                        }
                    }
                    catch (error) {
                        console.error(error);
                    }
                }


                /*********
                ** Add charts based on content-packs-by-format.jsonl
                *********/
                if (!this.contentPacks.loadFailed) {
                    try {
                        const curData = data.contentPacks;
                        const lastRowIndex = curData.rows.length - 1;

                        // 'content packs by format version' pie chart
                        {
                            const chart = echarts.init(document.getElementById("contentPacksByFormat"));

                            chart.setOption({
                                baseOption: {
                                    series: [createPieChartSeries()],
                                    timeline: createTimeline(curData.xLabels, config.contentPacks.notableEvents),
                                    toolbox: createToolbox(false)
                                },
                                options: curData.rows.map((row, i) => {
                                    const isLatest = i === lastRowIndex;
                                    const total = getSum(curData.versions, version => row[version]);

                                    return {
                                        title: {
                                            text: getTimelineTitle("Content packs by format version", row.date, isLatest),
                                            textStyle: createTitleStyle()
                                        },
                                        series: [
                                            {
                                                label: {
                                                    formatter: entry => `${entry.name}  ${getPercent(entry.value, total)}%`
                                                },
                                                data: curData.versions.map(version => ({
                                                    name: version,
                                                    value: row[version] ?? 0,
                                                    label: {
                                                        show: !!row[version]
                                                    },
                                                    itemStyle: {
                                                        color: getColor(config.contentPacks.colors, version)
                                                    }
                                                }))
                                            }
                                        ],
                                        tooltip: {
                                            formatter: entry => entry.name && entry.value
                                                ? `${entry.name}: ${entry.value.toLocaleString()} (${getPercent(entry.value, total)}%)`
                                                : null
                                        },
                                        animation: isLatest
                                    };
                                })
                            });

                            charts.push(chart);
                        }

                        // 'content packs by format version' line chart
                        {
                            // find versions that were ever in the top five
                            const showVersionsSet = new Set();
                            for (const row of curData.rows) {
                                for (const version of [...curData.versions].sort((a, b) => (row[b] ?? 0) - (row[a] ?? 0)).slice(0, 5))
                                    showVersionsSet.add(version);
                            }
                            const showVersions = Array.from(showVersionsSet);

                            // build chart
                            const chart = echarts.init(document.getElementById("contentPacksByFormatOverTime"));

                            const topVersions = new Set(
                                [...showVersions]
                                    .sort((a, b) => (curData.lastRow[b] ?? 0) - (curData.lastRow[a] ?? 0))
                                    .slice(0, 5)
                            );

                            chart.setOption({
                                title: {
                                    text: "Content packs by format version (top five)",
                                    textStyle: createTitleStyle()
                                },
                                legend: {
                                    show: false
                                },
                                grid: {
                                    top: 40,
                                    left: 60,
                                    right: 150 // make room for end labels
                                },
                                xAxis: {
                                    data: curData.xLabels,
                                    axisLabel: {
                                        rotate: 90,
                                        fontSize: 11,
                                        formatter: value => value.slice(0, 7)
                                    }
                                },
                                yAxis: {},
                                series: addMarkLine(
                                    showVersions.map(version => {
                                        const highlight = topVersions.has(version);
                                        const rawColor = getColor(config.contentPacks.colors, version);
                                        const color = highlight ? rawColor : hexToRgba(rawColor, 0.5);
                                        const seriesData = curData.rows.map(r => r[version] ?? null);

                                        return {
                                            type: "line",
                                            name: version,
                                            data: seriesData,
                                            color,
                                            symbol: "none",
                                            lineStyle: {
                                                type: highlight ? "solid" : "dotted"
                                            },
                                            endLabel: {
                                                show: !!seriesData[seriesData.length - 1],
                                                formatter: "{a}", // {a} = name
                                                color
                                            },
                                            labelLayout: {
                                                moveOverlap: "shiftY"
                                            }
                                        };
                                    }),
                                    p => p.name,
                                    config.contentPacks.notableEvents
                                ),
                                tooltip: {
                                    trigger: "axis"
                                },
                                toolbox: createToolbox(true)
                            });

                            charts.push(chart);
                        }
                    }
                    catch (error) {
                        console.error(error);
                    }
                }


                /*********
                ** Add charts based on dns-queries.json
                *********/
                if (!this.dnsQueries.loadFailed) {
                    try {
                        const curData = data.dnsQueries;

                        // 'DNS queries over time' bar chart
                        {
                            const chart = echarts.init(document.getElementById("dnsQueriesOverTime"));

                            chart.setOption({
                                title: {
                                    text: "Web traffic",
                                    textStyle: createTitleStyle()
                                },
                                grid: {
                                    top: 55,
                                    left: 70,
                                    right: 20
                                },
                                xAxis: {
                                    type: "category",
                                    data: curData.xLabels,
                                    axisLabel: {
                                        rotate: 90,
                                        fontSize: 11
                                    }
                                },
                                yAxis: {
                                    type: "value",
                                    name: "DNS queries (millions)",
                                    axisLabel: {
                                        formatter: value => (value / 1_000_000).toFixed(0)
                                    }
                                },
                                series: [
                                    {
                                        type: "bar",
                                        data: curData.values,
                                        color: "#3388cc",
                                        markLine: createMarkLine(p => p.name, config.dnsQueries.notableEvents)
                                    }
                                ],
                                tooltip: {
                                    trigger: "axis",
                                    formatter: params => {
                                        const value = params[0].value;

                                        const lines = [
                                            params[0].axisValue, // date
                                            value > 1_000_000
                                                ? `${(Math.round(value / 100_000) / 10).toLocaleString()} million`
                                                : value.toLocaleString()
                                        ];

                                        return lines.join("<br />");
                                    }
                                },
                                toolbox: createToolbox(true)
                            });

                            charts.push(chart);
                        }
                    }
                    catch (error) {
                        console.error(error);
                    }
                }


                /*********
                ** Add charts based on smapi-costs.json
                *********/
                if (!this.costs.loadFailed) {
                    try {
                        const curData = data.costs;

                        // 'SMAPI costs over time' stacked bar chart
                        {
                            const chart = echarts.init(document.getElementById("costsOverTime"));

                            chart.setOption({
                                title: {
                                    text: `SMAPI costs (${curData.xLabels[0]} through ${curData.xLabels[curData.xLabels.length - 1]})`,
                                    textStyle: createTitleStyle()
                                },
                                legend: {
                                    top: 25
                                },
                                grid: {
                                    top: 65,
                                    left: 60,
                                    right: 20
                                },
                                xAxis: {
                                    type: "category",
                                    data: curData.xLabels,
                                    axisLabel: {
                                        rotate: 90,
                                        fontSize: 11
                                    }
                                },
                                yAxis: {
                                    type: "value",
                                    name: "USD"
                                },
                                series: addMarkLine(
                                    curData.serviceKeys.map(key => ({
                                        type: "bar",
                                        name: key,
                                        stack: "total",
                                        data: curData.rows.map(row => Math.round((row[key] ?? 0) * 100) / 100),
                                        color: getColor(config.costs.colors, key)
                                    })),
                                    p => p.name,
                                    config.costs.notableEvents
                                ),
                                tooltip: {
                                    trigger: "axis",
                                    formatter: params => {
                                        const total = getSum(params, p => p.value);

                                        const lines = [
                                            params[0].axisValue, // date
                                            ...params
                                                .filter(p => p.value > 0)
                                                .map(p => `${p.marker}${p.seriesName}: $${p.value.toFixed(2)}`),
                                            `<strong>Total: US$${total.toFixed(2)}</strong>`
                                        ];

                                        return lines.join("<br />");
                                    }
                                },
                                toolbox: createToolbox(true)
                            });

                            charts.push(chart);
                        }
                    }
                    catch (error) {
                        console.error(error);
                    }
                }
            },

            /**
             * Format a numeric change for display, with comma separators and sign.
             * @param {number} value The number to format.
             * @returns {string} Returns the formatted number.
             */
            formatDelta: function (value) {
                return value > 0
                    ? `+${value.toLocaleString()}`
                    : value.toLocaleString();
            }
        }
    });


    /*********
    ** Helper methods
    *********/
    /****
    ** Generic data
    ****/
    /**
     * Fetch and parse data from a JSONL URI.
     * @param {string} fetchUri The URI from which to fetch the JSONL data.
     * @returns {null|array<object>} Returns an array of parsed objects if the data was found, else `null`.
     */
    async function fetchJsonLinesAsync(fetchUri) {
        const response = await fetch(fetchUri);
        if (!response.ok)
            return null;

        const rawJsonLines = await response.text();
        return rawJsonLines.trim().split(/\r?\n/).filter(row => !!row).map(JSON.parse);
    }

    /**
     * Fetch and parse data from a JSON URI.
     * @param {string} fetchUri The URI from which to fetch the JSON data.
     * @returns {null|object} Returns the parsed data if it was found, else `null`.
     */
    async function fetchJsonAsync(fetchUri) {
        const response = await fetch(fetchUri);
        if (!response.ok)
            return null;

        return await response.json();
    }

    /**
     * Get a formatted date string.
     * @param {Date|string} date The ISO 8601 date-only string to format.
     * @param {Intl.DateTimeFormatOptions} options The date format options.
     */
    function formatDate(date, options) {
        if (typeof date === "string")
            date = new Date(date + "T12:00"); // set time to noon to avoid any timezone conversions changing the date parts

        return new Intl.DateTimeFormat("en-GB", options).format(date);
    }

    /**
     * Get a formatted date string in the form 'dd MMM yyyy', like '31 January 2030'.
     * @param {Date|string} date The ISO 8601 date-only string to format.
     */
    function formatFullDate(date) {
        return formatDate(date, { dateStyle: "long" });
    }

    /**
     * Get a formatted month string in the form 'Month YYYY', like 'January 2030'.
     * @param {string} monthDateStr The month date string in the form 'YYYY-MM'.
     */
    function formatMonthYear(monthDateStr) {
        return formatDate(monthDateStr + "-01", { month: "long", year: "numeric" });
    }

    /**
     * Get the display name for a data key.
     * @param {object} lookup The name dictionary to check.
     * @param {string} key The data key.
     * @returns {string}
     */
    function getLabel(lookup, key) {
        return lookup[key.toLowerCase()] ?? key;
    }

    /**
     * Get the hexadecimal color code for a data key.
     * @param {object} lookup The color dictionary to check.
     * @param {string} key The data key.
     * @returns {string}
     */
    function getColor(lookup, key) {
        return lookup[key.toLowerCase()] ?? "#808080";
    }

    /**
     * Get a single-decimal percentage value.
     * @param {number|null|undefined} value The value for which to get a percentage.
     * @param {number} total The denominator for the value.
     * @returns {string}
     */
    function getPercent(value, total) {
        if (!value)
            value = 0;

        return (Math.round(value / total * 1000) / 10).toString();
    }

    /**
     * Get the sum of a function over all values.
     * @param {array<string|number>} values The values for which to get a sum.
     * @param {(any) => number|null} getValue Get the value to sum for an entry.
     * @returns {number}
     */
    function getSum(values, getValue) {
        return values.reduce((sum, value) => sum + (getValue(value) ?? 0), 0);
    }

    /**
     * Get an RGBA representation of a hexadecimal color.
     * @param {string} hex The hexadecimal color code, including the '#' prefix.
     * @param {number} alpha The alpha channel, as a value between 0 (transparent) and 1 (opaque).
     * @returns
     */
    function hexToRgba(hex, alpha) {
        const r = parseInt(hex.slice(1, 3), 16);
        const g = parseInt(hex.slice(3, 5), 16);
        const b = parseInt(hex.slice(5, 7), 16);
        return `rgba(${r},${g},${b},${alpha})`;
    }

    /**
     * Get a copy of an object with all its keys lowercased.
     * @param {object} row The object to map.
     * @returns {object}
     */
    function lowercaseKeys(row) {
        const copy = {};

        for (const [key, value] of Object.entries(row))
            copy[key.toLowerCase()] = value;

        return copy;
    }

    /**
     * Get the unique keys from a column-per-key row, sorted by their value in the latest row.
     * @param {array<object>} rows The parsed rows over time.
     * @returns {array<string>}
     */
    function getModKeys(rows) {
        const rawModTypes = rows.flatMap(row => Object.keys(row).filter(isDataField));
        const uniqueModTypes = [...new Set(rawModTypes)];

        const lastRow = rows[rows.length - 1];
        return uniqueModTypes.sort((modTypeA, modTypeB) => {
            // special case: 'other' always at the end
            if (modTypeA === "other")
                return 1;
            if (modTypeB === "other")
                return -1;

            // else sort by count descending
            const countA = lastRow[modTypeA] ?? 0;
            const countB = lastRow[modTypeB] ?? 0;
            return countB - countA;
        });
    }

    /**
     * Assign colors to each key in a color map based on the color palette.
     * @param {object} colorMap A lookup of key to color.
     * @param {array<string>} keys The keys for which to assign colors.
     */
    function assignColors(colorMap, keys) {
        let index = 0;
        for (const rawKey of keys) {
            const key = rawKey.toLowerCase();

            if (!colorMap[key])
                colorMap[key] = config.colorPalette[index++ % config.colorPalette.length];
        }
    }

    /**
     * Get whether a field name in a dated row is metadata (like a date or comment) rather than one of the actual data fields.
     * @param {string} key The data key to check.
     * @returns {boolean}
     */
    function isMetaField(key) {
        return key === "date" || key === "@comment";
    }

    /**
     * Get whether a field name in a dated row is a data value, instead of a metadata field (like a date or comment).
     * @param {string} key The data key to check.
     * @returns {boolean}
     */
    function isDataField(key) {
        return !isMetaField(key);
    }

    /****
    ** Mods-by-type data
    ****/
    /**
     * Get whether a key in a data row is a strict mod type, rather than a metadata field (like 'date' or '@comment') or 'other'.
     * @param {string} key The data key to check.
     * @returns {boolean}
     */
    function isSpecificModType(key) {
        return isDataField(key) && key !== "other";
    }

    /****
    ** Charts
    ****/
    /**
     * Get the chart title for a given timeline frame.
     * @param {string} baseTitle The base chart title.
     * @param {string|Date} date The date of the current timeline frame.
     * @param {boolean} isLatest Whether the chart is displaying the last frame on the timeline.
     * @returns {string}
     */
    function getTimelineTitle(baseTitle, date, isLatest) {
        return isLatest
            ? baseTitle
            : `${baseTitle} (${formatFullDate(date)})`;
    }

    /**
     * Create the default chart title style.
     */
    function createTitleStyle() {
        return {
            fontFamily: "Roboto",
            fontSize: 13
        };
    }

    /**
     * Create the default series configuration for a pie chart.
     * @returns {object}
     */
    function createPieChartSeries() {
        return {
            type: "pie",
            radius: "80%",
            clockwise: false // start with larger values on the left
        };
    }

    /**
     * Create the timeline options for a chart, given its x-axis labels and optional date markers.
     * @param {array<string>} xLabels The x-axis labels.
     * @param {array<object>} markers The date markers to display on the timeline, if any. This should be a lookup of `xLabels` key to tooltip text.
     * @param {object|null} customOptions the custom ECharts timeline options to merge into the default options, if any.
     */
    function createTimeline(xLabels, markers, customOptions) {
        return {
            axisType: "category",
            data: xLabels.map(xLabel => {
                const marker = markers[xLabel];
                return marker
                    ? {
                        value: xLabel,
                        symbol: "diamond",
                        tooltip: {
                            formatter: () => marker
                        }
                    }
                    : {
                        value: xLabel,
                        symbol: "none"
                    };
            }),
            currentIndex: xLabels.length - 1, // default to latest row
            playInterval: 200,
            loop: false,
            realtime: true,
            label: {
                show: false
            },
            ...customOptions
        };
    }

    /**
     * Create the toolbox options for a chart.
     * @param {boolean} isLinearChart Whether the options are for a linear chart (e.g. a bar chart or line chart).
     */
    function createToolbox(isLinearChart) {
        return {
            feature: {
                dataZoom: {
                    show: isLinearChart
                },
                saveAsImage: {
                    excludeComponents: ["timeline", "toolbox"]
                }
            }
        };
    }

    /**
     * Create a 'mark line', which can be added to a series to show annotations for specific dates.
     * @param {(any) => string} formatter Get the value to display.
     * @param {object} events A lookup of xAxis label to the annotation to display.
     * @returns {object}
     */
    function createMarkLine(formatter, events) {
        return {
            silent: true,
            symbol: "none",
            lineStyle: {
                color: "#888"
            },
            label: {
                formatter,
                color: "#666",
                fontSize: 11
            },
            data: Object.entries(events).map(([xAxis, name], i) => ({
                xAxis,
                name,
                label: {
                    offset: i % 2 === 1
                        ? [0, 16] // shift every second label down to minimize overlapping text
                        : [0, 0]
                }
            }))
        };
    }

    /**
     * Add a 'mark line' (which shows annotations for specific dates) to a chart's series.
     * @param {array<object>} series The chart series to extend.
     * @param {(any) => string} formatter Get the value to display.
     * @param {object} events A lookup of xAxis label to the annotation to display.
     * @returns {array<object>}
     */
    function addMarkLine(series, formatter, events) {
        if (series.length > 0)
            series[0].markLine = createMarkLine(formatter, events);

        return series;
    }

    /**
     * Resize all charts to match their container size.
     * @param {array<object>} charts The charts to resize.
     */
    function resizeCharts(charts) {
        for (const chart of charts)
            chart.resize();
    }
};
