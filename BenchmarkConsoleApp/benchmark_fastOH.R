####### Benchmarking ########

load_raw <- function(rawpath) {
  raw <- read.table(rawpath, skip = 1) # First line is headers
  ids <- data.frame(ID = as.vector(raw[, 2]), ordercode = seq_len(nrow(raw)), stringsAsFactors = F)
  dat <- data.matrix(raw[, -1:-6])
  cat("\n...", nrow(dat), "samples and", ncol(dat), "markers read from file ...\n\n")
  rownames(dat) <- ids$ID
  return(list(zygosities = dat, ids = ids))
}

fastOH2 <- function(A, B) {
  #' opposite homozygote count matrix - Ferdosi M. and Boerner V. (2014)
  AtB <- A %*% t(B) # multiply A with the transpose of B
  return(AtB + t(AtB))
}

# memory.limit(9999999999) # Needed for the 300+30000 dataset

w <- 1
n <- 20

for (file in c(
    "Datasets/1000+1000 samples 15000 markers.raw",
    "Datasets/1000+5000 samples 15000 markers.raw",
    "Datasets/3000+3000 samples 15000 markers.raw",
    "Datasets/3000+3000 samples all markers.raw"
    # "Datasets/300+30000 samples all markers.raw"
    )) {
    cat("Loading raw file", file, "...")
    raw <- load_raw(file)
    zygosities <- raw$zygosities
    cat("... raw file loaded. Starting OH counts ...\n")
    A <- +(zygosities == 0) # 0 maps to 1.0, all other values map to 0.0
    B <- +(zygosities == 2) # 2 maps to 1.0, all other values map to 0.0
    if (w > 0) {
        for(i in 1:w) { # warmup
            start_time <- Sys.time()
            OH <- fastOH2(A, B)
            # OH <- fastOHoriginal(zygosities)
            elapsed <- difftime(Sys.time(), start_time, units = "secs")
            cat("... warmup OH counts", i, "calculated in", round(elapsed, 3), "seconds ...\n")
        }
    }

    sum <- 0
    for (i in 1:n) {
        gc()
        start_time <- Sys.time()
        OH <- fastOH2(A, B)
        # OH <- fastOHoriginal(zygosities)
        elapsed <- difftime(Sys.time(), start_time, units = "secs")
        cat("... OH counts", i, "calculated in", round(elapsed, 3), "seconds ...\n")
        sum <- sum + elapsed
    }
    cat("... OH counting took an average of", sum / n, "seconds per computation.\n")
}