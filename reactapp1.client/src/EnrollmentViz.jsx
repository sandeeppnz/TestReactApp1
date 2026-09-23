const palette = ['#2563eb', '#dc2626', '#16a34a', '#d97706', '#7c3aed', '#0891b2'];

function EnrollmentTable({ rows }) {
    return (
        <div className="viz-scroll">
            <table className="viz-table">
                <thead>
                    <tr>
                        <th>Year</th>
                        <th>Programme</th>
                        <th>Faculty</th>
                        <th>Students</th>
                    </tr>
                </thead>
                <tbody>
                    {rows.map((row, index) =>
                        <tr key={index}>
                            <td>{row.year}</td>
                            <td>{row.programme}</td>
                            <td>{row.faculty}</td>
                            <td>{row.studentCount}</td>
                        </tr>
                    )}
                </tbody>
            </table>
        </div>
    );
}

function EnrollmentLineChart({ rows }) {
    const width = 480;
    const height = 220;
    const padding = { top: 16, right: 16, bottom: 28, left: 40 };
    const innerWidth = width - padding.left - padding.right;
    const innerHeight = height - padding.top - padding.bottom;

    const programmes = [...new Set(rows.map(r => r.programme))];
    const years = [...new Set(rows.map(r => r.year))].sort((a, b) => a - b);
    const maxCount = Math.max(...rows.map(r => r.studentCount), 1);

    const xFor = year => padding.left + (years.length > 1
        ? (years.indexOf(year) / (years.length - 1)) * innerWidth
        : innerWidth / 2);
    const yFor = count => padding.top + innerHeight - (count / maxCount) * innerHeight;

    return (
        <svg className="viz-chart" viewBox={`0 0 ${width} ${height}`} role="img" aria-label="Enrollment trend chart">
            <line x1={padding.left} y1={padding.top} x2={padding.left} y2={height - padding.bottom} stroke="#ccc" />
            <line x1={padding.left} y1={height - padding.bottom} x2={width - padding.right} y2={height - padding.bottom} stroke="#ccc" />

            {years.map(year =>
                <text key={year} x={xFor(year)} y={height - padding.bottom + 16} fontSize="10" textAnchor="middle" fill="#555">
                    {year}
                </text>
            )}
            <text x={padding.left - 6} y={padding.top + 4} fontSize="10" textAnchor="end" fill="#555">{maxCount}</text>
            <text x={padding.left - 6} y={height - padding.bottom} fontSize="10" textAnchor="end" fill="#555">0</text>

            {programmes.map((programme, i) => {
                const points = rows
                    .filter(r => r.programme === programme)
                    .sort((a, b) => a.year - b.year);
                const color = palette[i % palette.length];

                return (
                    <g key={programme}>
                        <polyline
                            points={points.map(p => `${xFor(p.year)},${yFor(p.studentCount)}`).join(' ')}
                            fill="none"
                            stroke={color}
                            strokeWidth="2"
                        />
                        {points.map(p =>
                            <circle key={p.year} cx={xFor(p.year)} cy={yFor(p.studentCount)} r="2.5" fill={color} />
                        )}
                    </g>
                );
            })}

            {programmes.length > 1 &&
                <g>
                    {programmes.map((programme, i) =>
                        <g key={programme} transform={`translate(${padding.left + i * 130}, 4)`}>
                            <rect width="8" height="8" fill={palette[i % palette.length]} />
                            <text x="12" y="8" fontSize="10" fill="#333">{programme}</text>
                        </g>
                    )}
                </g>
            }
        </svg>
    );
}

function EnrollmentBarChart({ rows }) {
    const width = 480;
    const height = 240;
    const padding = { top: 20, right: 16, bottom: 56, left: 40 };
    const innerWidth = width - padding.left - padding.right;
    const innerHeight = height - padding.top - padding.bottom;
    const maxCount = Math.max(...rows.map(r => r.studentCount), 1);

    const distinctYears = new Set(rows.map(r => r.year));
    const labelFor = row => distinctYears.size > 1 ? `${row.programme} (${row.year})` : row.programme;

    const gap = innerWidth / rows.length;
    const barWidth = Math.min(gap * 0.6, 48);

    return (
        <svg className="viz-chart" viewBox={`0 0 ${width} ${height}`} role="img" aria-label="Enrollment comparison chart">
            <line x1={padding.left} y1={padding.top} x2={padding.left} y2={height - padding.bottom} stroke="#ccc" />
            <line x1={padding.left} y1={height - padding.bottom} x2={width - padding.right} y2={height - padding.bottom} stroke="#ccc" />

            {rows.map((row, i) => {
                const barHeight = (row.studentCount / maxCount) * innerHeight;
                const x = padding.left + i * gap + (gap - barWidth) / 2;
                const y = height - padding.bottom - barHeight;
                const labelY = height - padding.bottom + 12;
                const labelX = x + barWidth / 2;

                return (
                    <g key={i}>
                        <rect x={x} y={y} width={barWidth} height={barHeight} fill={palette[i % palette.length]} />
                        <text x={labelX} y={y - 4} fontSize="9" textAnchor="middle" fill="#333">{row.studentCount}</text>
                        <text
                            x={labelX}
                            y={labelY}
                            fontSize="9"
                            textAnchor="end"
                            fill="#555"
                            transform={`rotate(-35 ${labelX} ${labelY})`}
                        >
                            {labelFor(row)}
                        </text>
                    </g>
                );
            })}
        </svg>
    );
}

export function EnrollmentViz({ chartType, rows }) {
    if (!rows || rows.length < 2 || chartType === 'none' || !chartType) {
        return null;
    }

    return (
        <div className="viz-container">
            {chartType === 'line' && <EnrollmentLineChart rows={rows} />}
            {chartType === 'bar' && <EnrollmentBarChart rows={rows} />}
            {chartType === 'table' && <EnrollmentTable rows={rows} />}
        </div>
    );
}
