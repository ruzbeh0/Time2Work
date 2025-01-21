import React, { useState, useEffect, useRef, useCallback, FC } from 'react';
import classNames from 'classnames';
import styles from './Panel.module.scss';

interface PanelProps {
    children: React.ReactNode;
    title: string;
    style?: React.CSSProperties;
    initialPosition?: { top: number; left: number };
    initialSize?: { width: number; height: number };
    onPositionChange?: (newPosition: { top: number; left: number }) => void;
    onSizeChange?: (newSize: { width: number; height: number }) => void;
    className?: string;
    onClose?: () => void;  // Make onClose optional
    savedPosition?: { top: number; left: number };
    savedSize?: { width: number; height: number };
    onSavePosition?: (position: { top: number; left: number }) => void;
    onSaveSize?: (size: { width: number; height: number }) => void;
}

type InteractionState = 'none' | 'dragging' | 'resizing';

const Panel: FC<PanelProps> = ({
    children,
    title,
    style,
    initialPosition = { top: 100, left: 10 },
    initialSize = { width: 300, height: 600 },
    onPositionChange = () => {},
    onSizeChange = () => {},
    className = '',
    onClose,  // Add onClose to the props
    savedPosition,
    savedSize,
    onSavePosition = () => {},
    onSaveSize = () => {},
}) => {
    // State for position and size
    const [position, setPosition] = useState(savedPosition || initialPosition);
    const [size, setSize] = useState(savedSize || initialSize);

    // State for interaction (dragging/resizing) and relative cursor position
    const [interaction, setInteraction] = useState<{
        state: InteractionState;
        rel?: { x: number; y: number };
        initialSize?: { width: number; height: number };
    }>({ state: 'none' });

    // Refs
    const panelRef = useRef<HTMLDivElement>(null);
    const contentRef = useRef<HTMLDivElement>(null);

    // Handler for mouse down on header (start dragging)
    const handleDragMouseDown = useCallback((e: React.MouseEvent<HTMLDivElement>) => {
        if (e.button !== 0) return; // Only left mouse button
        const rect = panelRef.current?.getBoundingClientRect();
        if (!rect) return;
        setInteraction({
            state: 'dragging',
            rel: { x: e.clientX - rect.left, y: e.clientY - rect.top },
        });
        e.stopPropagation();
        e.preventDefault();
    }, []);

    // Handler for mouse down on resizer (start resizing)
    const handleResizeMouseDown = useCallback((e: React.MouseEvent<HTMLDivElement>) => {
        if (e.button !== 0) return; // Only left mouse button
        setInteraction({
            state: 'resizing',
            rel: { x: e.clientX, y: e.clientY },
            initialSize: { ...size },
        });
        e.stopPropagation();
        e.preventDefault();
    }, [size]);

    // Unified mouse move handler
    const handleMouseMove = useCallback((e: MouseEvent) => {
        if (interaction.state === 'dragging') {
            const newLeft = e.clientX - (interaction.rel?.x || 0);
            const newTop = e.clientY - (interaction.rel?.y || 0);

            // Constrain within viewport
            const maxLeft = window.innerWidth - size.width;
            const maxTop = window.innerHeight - size.height;

            const clampedLeft = Math.min(Math.max(newLeft, 0), maxLeft);
            const clampedTop = Math.min(Math.max(newTop, 0), maxTop);

            const newPosition = { top: clampedTop, left: clampedLeft };
            setPosition(newPosition);
            onPositionChange(newPosition);
            onSavePosition(newPosition);
        } else if (interaction.state === 'resizing') {
            const deltaX = e.clientX - (interaction.rel?.x || 0);
            const deltaY = e.clientY - (interaction.rel?.y || 0);

            let newWidth = (interaction.initialSize?.width || size.width) + deltaX;
            let newHeight = (interaction.initialSize?.height || size.height) + deltaY;

            // Set minimum size constraints
            newWidth = Math.max(newWidth, 200); // Minimum width
            newHeight = Math.max(newHeight, 300); // Minimum height

            // Optionally, set maximum size based on viewport
            newWidth = Math.min(newWidth, window.innerWidth - position.left);
            newHeight = Math.min(newHeight, window.innerHeight - position.top);

            const newSize = { width: newWidth, height: newHeight };
            setSize(newSize);
            onSizeChange(newSize);
            onSaveSize(newSize);
        }
    }, [interaction, size.width, size.height, position.left, position.top, onPositionChange, onSizeChange, onSavePosition, onSaveSize]);

    // Mouse up handler to end interaction
    const handleMouseUp = useCallback(() => {
        if (interaction.state !== 'none') {
            setInteraction({ state: 'none' });
        }
    }, [interaction.state]);

    // Attach global mouse move and mouse up listeners when interacting
    useEffect(() => {
        if (interaction.state === 'none') return;

        window.addEventListener('mousemove', handleMouseMove);
        window.addEventListener('mouseup', handleMouseUp);

        return () => {
            window.removeEventListener('mousemove', handleMouseMove);
            window.removeEventListener('mouseup', handleMouseUp);
        };
    }, [interaction.state, handleMouseMove, handleMouseUp]);

    // Function to dynamically adjust font size based on panel size
    const adjustFontSize = useCallback(() => {
        if (contentRef.current) {
            // Calculate font size based on the panel's width and height
            const fontSize = Math.max(Math.min(size.width * 0.02, size.height * 0.02), 8); // Clamp between 8px and 2% of width/height
            contentRef.current.style.fontSize = `${fontSize}px`;
        }
    }, [size]);

    // Adjust font size whenever size changes
    useEffect(() => {
        adjustFontSize();
    }, [size, adjustFontSize]);

    return (
        <div
            ref={panelRef}
            style={{
                position: 'absolute',
                top: position.top,
                left: position.left,
                width: size.width,
                height: size.height,
                backgroundColor: 'var(--panelColorNormal)',
                border: '1px solid #444',
                borderRadius: '8px',
                boxShadow: '0 4px 8px rgba(0, 0, 0, 0.2)',
                overflow: 'hidden',
                display: 'flex',
                flexDirection: 'column',
                ...style,
            }}
            className={className}
        >
            <div className={styles.header} onMouseDown={handleDragMouseDown}>
                <span>{title}</span>
                {onClose && (
                    <button
                        className={classNames(
                            styles.exitbutton,
                            "button_bvQ button_bvQ close-button_wKK",
                        )}
                        onClick={onClose}
                    >
                        <div
                            className="tinted-icon_iKo"
                            style={{
                                maskImage: "url(Media/Glyphs/Close.svg)",
                                width: "var(--iconWidth)",
                                height: "var(--iconHeight)",
                            }}
                        ></div>
                    </button>
                )}
            </div>

            {/* Content */}
            <div
                ref={contentRef}
                style={{
                    flex: '1 1 auto',
                    padding: '10px',
                    overflow: 'auto',
                    display: 'flex',
                    flexDirection: 'column',
                    background: 'linear-gradient(135deg, rgba(30, 30, 30, 0.95), rgba(45, 45, 45, 0.95))',
                    ...style,
                }}
            >
                {children}
            </div>

            {/* Resizer */}
            <div
                style={{
                    position: 'absolute',
                    bottom: 0,
                    right: 0,
                    width: '20px',
                    height: '20px',
                    cursor: 'nwse-resize',
                    background: 'linear-gradient(45deg, transparent 50%, white 50%)',
                }}
                onMouseDown={handleResizeMouseDown}
            />
        </div>
    );
};

export default Panel;
