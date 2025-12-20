import {activityTagName, createActivityElement} from "../internal/create-activity-element";
import {Activity, Size} from "../models";

// Cache for storing calculated activity sizes
// Key format: "activityType:showsDescription:hasIcon"
const sizeCache = new Map<string, Size>();

// Queue for batching size calculations
let calculationQueue: Array<{activity: Activity, resolve: (size: Size) => void, reject: (error: any) => void}> = [];
let batchTimer: number | null = null;

function getCacheKey(activity: Activity): string {
    const type = activity.type || 'unknown';
    const hasDescription = !!activity.metadata?.description;
    const showDescription = activity.metadata?.showDescription === true;
    const hasIcon = !!activity.metadata?.icon;
    // Simplified key - can be enhanced to include port count if needed
    return `${type}:${hasDescription && showDescription}:${hasIcon}`;
}

function processBatch() {
    if (calculationQueue.length === 0) {
        return;
    }

    const batch = calculationQueue;
    calculationQueue = [];
    batchTimer = null;

    // Create a single container for all measurements
    const container = document.createElement('div');
    container.style.position = 'absolute';
    container.style.visibility = 'hidden';
    container.style.left = '-9999px';
    container.style.top = '-9999px';
    
    const bodyElement = document.getElementsByTagName('body')[0];
    bodyElement.appendChild(container);

    const measurements: Array<{wrapper: HTMLElement, activity: Activity, resolve: (size: Size) => void, reject: (error: any) => void}> = [];

    // Create all elements at once
    for (const item of batch) {
        const cacheKey = getCacheKey(item.activity);
        
        // Check cache first
        const cachedSize = sizeCache.get(cacheKey);
        if (cachedSize) {
            item.resolve(cachedSize);
            continue;
        }

        const wrapper = document.createElement('div');
        wrapper.style.display = 'inline-block';
        const dummyActivityElement = createActivityElement(item.activity, true);
        wrapper.appendChild(dummyActivityElement);
        container.appendChild(wrapper);
        
        measurements.push({
            wrapper,
            activity: item.activity,
            resolve: item.resolve,
            reject: item.reject
        });
    }

    if (measurements.length === 0) {
        container.remove();
        return;
    }

    // Wait for all elements to render
    const checkAllSizes = () => {
        let allReady = true;
        
        for (const measurement of measurements) {
            const activityElement = measurement.wrapper.getElementsByTagName(activityTagName)[0];
            
            if (!activityElement) {
                // Activity element not yet present; batch is not ready.
                allReady = false;
                continue;
            }
            
            const activityElementRect = activityElement.getBoundingClientRect();
            
            // If any element is not ready, continue waiting
            if (activityElementRect.width === 0 || activityElementRect.height === 0) {
                allReady = false;
                break;
            }
        }

        if (!allReady) {
            window.requestAnimationFrame(checkAllSizes);
            return;
        }

        // All elements are ready, measure them all
        for (const measurement of measurements) {
            try {
                const rect = measurement.wrapper.getElementsByClassName("elsa-activity")[0]?.getBoundingClientRect();
                
                if (!rect) {
                    measurement.reject('Activity element not found.');
                    continue;
                }
                
                const size: Size = {
                    width: rect.width,
                    height: rect.height
                };
                
                // Cache the size
                const cacheKey = getCacheKey(measurement.activity);
                sizeCache.set(cacheKey, size);
                
                measurement.resolve(size);
            } catch (error) {
                measurement.reject(error);
            }
        }

        // Clean up
        container.remove();
    };

    // Start checking
    checkAllSizes();
}

export function calculateActivitySize(activity: Activity): Promise<Size> {
    // Check cache first
    const cacheKey = getCacheKey(activity);
    const cachedSize = sizeCache.get(cacheKey);
    
    if (cachedSize) {
        return Promise.resolve(cachedSize);
    }

    // Add to batch queue
    return new Promise((resolve, reject) => {
        calculationQueue.push({activity, resolve, reject});

        // Schedule batch processing
        if (batchTimer === null) {
            batchTimer = window.setTimeout(processBatch, 0) as any;
        }
    });
}

// Export function to clear cache if needed
export function clearActivitySizeCache() {
    sizeCache.clear();
}